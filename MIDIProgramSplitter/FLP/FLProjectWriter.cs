using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLProjectWriter
{
	/// <summary>The project was started on 26/4/23 17:20. Total time spent on it: 4 minutes? This is probably the wrong file</summary>
	private static ReadOnlySpan<byte> ProjectTime_Default => new byte[16] { 0xC5, 0x8D, 0x96, 0x1D, 0x57, 0xFE, 0xE5, 0x40, 0x00, 0x00, 0x00, 0x50, 0xAA, 0x93, 0x25, 0x3F };

	private static ReadOnlySpan<byte> MIDIInfo0 => new byte[20] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x90, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0xD5, 0x01, 0x00, 0x00 };
	private static ReadOnlySpan<byte> MIDIInfo1 => new byte[20] { 0xFD, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x90, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0xD5, 0x01, 0x00, 0x00 };
	private static ReadOnlySpan<byte> MIDIInfo2 => new byte[20] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0xFF };

	// 0x0C if unmuted, 0x04 if muted.
	private static ReadOnlySpan<byte> FXParams_InsertMasterAndCurrent => new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	// 0x44 0x00 if Insert1 is muted. 0x4C 0x04 if unmuted and separator. The 0x4_ is probably "dock middle"
	private static ReadOnlySpan<byte> FXParams_Insert1Through125 => new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x4C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

	private static ReadOnlySpan<byte> FruityLSD_PluginParams => new byte[97]
	{
		0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00
	};
	private static ReadOnlySpan<byte> FruityLSD_NewPlugin => new byte[52]
	{
		0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x44, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0xD3, 0x03, 0x00, 0x00, 0x37, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00
	};

	// 56364 bytes
	// _initCtrlRecChan[9] is MainVolume. 50% => 25, 100% => 50, 125% => 62
	// Insert volumes are stored in here as well.
	private static readonly byte[] _initCtrlRecChan = File.ReadAllBytes("../../../../InitCtrlRecChan.bin");

	public readonly List<FLChannel> Channels;
	public readonly List<FLAutomation> Automations;
	public readonly List<FLPattern> Patterns;
	public readonly List<FLPlaylistItem> PlaylistItems;
	public readonly List<FLPlaylistMarker> PlaylistMarkers;
	public readonly List<FLPlaylistTrack> PlaylistTracks;
	public ushort PPQN;
	public decimal CurrentTempo;
	public byte TimeSigNumerator;
	public byte TimeSigDenominator;

	public FLProjectWriter(ushort ppqn = 96)
	{
		PPQN = ppqn;
		CurrentTempo = 120; // MIDI Default
		TimeSigNumerator = 4;
		TimeSigDenominator = 4;

		Channels = new List<FLChannel>();
		Automations = new List<FLAutomation>();
		Patterns = new List<FLPattern>();
		PlaylistItems = new List<FLPlaylistItem>();
		PlaylistMarkers = new List<FLPlaylistMarker>();

		PlaylistTracks = new List<FLPlaylistTrack>(500);
		for (int i = 0; i < PlaylistTracks.Capacity; i++)
		{
			PlaylistTracks.Add(new FLPlaylistTrack());
		}
	}

	public void AddToPlaylist(FLPattern p, uint tick, uint duration, FLPlaylistTrack track)
	{
		PlaylistItems.Add(new FLPlaylistItem(tick, p, duration, track));
	}
	public void AddToPlaylist(FLAutomation a, uint tick, uint duration, FLPlaylistTrack track)
	{
		PlaylistItems.Add(new FLPlaylistItem(tick, a, duration, track));
	}
	public void AddTimeSigMarker(uint tick, byte num, byte denom)
	{
		PlaylistMarkers.Add(new FLPlaylistMarker(tick, num + "/" + denom, (num, denom)));
	}

	public void Write(Stream s)
	{
		var w = new EndianBinaryWriter(s, ascii: true);

		// Header chunk
		w.WriteChars("FLhd");
		w.WriteUInt32(6); // Length
		w.WriteUInt16(0); // Format
		w.WriteUInt16((ushort)(Channels.Count + Automations.Count));
		w.WriteUInt16(PPQN);

		// Data chunk
		w.WriteChars("FLdt");

		long dataLenOffset = s.Position;
		w.WriteUInt32(0); // Write length later

		WriteProjectInfo(w);
		WriteChannels(w);
		WriteArrangement(w, 0);
		WriteMoreStuffIDK(w);
		WriteInsertMaster(w);
		WriteInsert1(w); // Different because of Fruity LSD
		for (int i = 0; i < 124; i++)
		{
			WriteInsert2Through125(w);
		}
		WriteInsertCurrent(w);
		WriteEnding(w);

		// Write data chunk length
		uint length = (uint)(s.Length - (dataLenOffset + 4));
		s.Position = dataLenOffset;
		w.WriteUInt32(length);
	}

	public static void WriteByteEvent(EndianBinaryWriter w, FLEvent ev, byte data)
	{
		w.WriteEnum(ev);
		w.WriteByte(data);
	}
	public static void WriteWordEvent(EndianBinaryWriter w, FLEvent ev, ushort data)
	{
		w.WriteEnum(ev);
		w.WriteUInt16(data);
	}
	public static void WriteDWordEvent(EndianBinaryWriter w, FLEvent ev, uint data)
	{
		w.WriteEnum(ev);
		w.WriteUInt32(data);
	}
	public static void WriteTextEventLength(EndianBinaryWriter w, uint length)
	{
		// TODO: How many bytes can this len use?
		while (true)
		{
			if (length <= 0x7F)
			{
				w.WriteByte((byte)length);
				return;
			}

			w.WriteByte((byte)((length & 0x7Fu) | 0x80u));
			length >>= 7;
		}
	}
	public static void WriteUTF8EventWithLength(EndianBinaryWriter w, FLEvent ev, string str)
	{
		WriteBytesEventWithLength(w, ev, Encoding.UTF8.GetBytes(str));
	}
	public static void WriteUTF16EventWithLength(EndianBinaryWriter w, FLEvent ev, string str)
	{
		WriteBytesEventWithLength(w, ev, Encoding.Unicode.GetBytes(str));
	}
	public static void WriteBytesEventWithLength(EndianBinaryWriter w, FLEvent ev, ReadOnlySpan<byte> bytes)
	{
		w.WriteEnum(ev);
		WriteTextEventLength(w, (uint)bytes.Length);
		w.WriteBytes(bytes);
	}

	private void WriteProjectInfo(EndianBinaryWriter w)
	{
		WriteUTF8EventWithLength(w, FLEvent.Version, "20.9.2.2963\0");
		WriteDWordEvent(w, FLEvent.VersionBuildNumber, 2963);
		WriteByteEvent(w, FLEvent.IsRegistered, 1); // TODO: Probably should make it 0 to avoid legal trouble? How does it authenticate this?
		WriteByteEvent(w, FLEvent.Unk_37, 1); // Authentication related too?
		WriteUTF16EventWithLength(w, FLEvent.RegistrationID, "d3@?4xufs49p1n?B>;?889\0"); // Probably shouldn't include this?

		WriteDWordEvent(w, FLEvent.FineTempo, (uint)(CurrentTempo * 1_000));
		WriteWordEvent(w, FLEvent.SelectedPatternNum, 1);
		WriteByteEvent(w, FLEvent.IsSongMode, 1);
		WriteByteEvent(w, FLEvent.Shuffle, 0);
		WriteWordEvent(w, FLEvent.MasterPitch, 0);
		WriteByteEvent(w, FLEvent.ProjectTimeSigNumerator, TimeSigNumerator);
		WriteByteEvent(w, FLEvent.ProjectTimeSigDenominator, TimeSigDenominator);
		WriteByteEvent(w, FLEvent.ProjectShouldUseTimeSignatures, 1);
		WriteByteEvent(w, FLEvent.PanningLaw, 0); // Circular
		WriteByteEvent(w, FLEvent.ShouldPlayTruncatedClipNotes, 1);
		WriteByteEvent(w, FLEvent.ShouldShowInfoOnOpen, 0);
		WriteUTF16EventWithLength(w, FLEvent.ProjectTitle, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectGenre, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectAuthor, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectDataPath, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectComment, "\0");
		// ProjectURL would go here
		WriteBytesEventWithLength(w, FLEvent.ProjectTime, ProjectTime_Default);

		WriteChanFilters(w);
		WritePatterns(w);

		// No idea what these mean, but there are 3 and they're always the same in all my projects.
		// Dunno if it's MIDI keyboard related or something, because I only have 1
		WriteBytesEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo0);
		WriteBytesEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo1);
		WriteBytesEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo2);
	}
	private void WriteChanFilters(EndianBinaryWriter w)
	{
		if (Automations.Count > 0)
		{
			WriteUTF16EventWithLength(w, FLEvent.ChanFilterName, "Automation\0");
			WriteUTF16EventWithLength(w, FLEvent.ChanFilterName, "Unsorted\0");
			WriteDWordEvent(w, FLEvent.CurFilterNum, 1);
		}
		else
		{
			WriteUTF16EventWithLength(w, FLEvent.ChanFilterName, "Unsorted\0");
			WriteDWordEvent(w, FLEvent.CurFilterNum, 0);
		}
	}
	private void WritePatterns(EndianBinaryWriter w)
	{
		if (Patterns.Count == 0)
		{
			WriteBytesEventWithLength(w, FLEvent.CtrlRecChan, Array.Empty<byte>());
		}
		else
		{
			// TODO: Why did this CtrlRecChan only show up sometimes?
			// Bytes: CtrlRecChan - 12 = [0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00]
			WriteBytesEventWithLength(w, FLEvent.CtrlRecChan, Array.Empty<byte>()); // This appeared when resizing a pattern without changing it
			for (int i = 0; i < Patterns.Count; i++)
			{
				WriteWordEvent(w, FLEvent.NewPattern, (ushort)(i + 1));
				Patterns[i].WritePatternNotes(w);
			}
		}
	}
	private void WriteChannels(EndianBinaryWriter w)
	{
		for (int i = 0; i < Automations.Count; i++)
		{
			Automations[i].WriteAutomationConnection(w, (ushort)(Channels.Count + i), Channels);
		}

		uint chanFilter = Automations.Count == 0 ? 0u : 1;
		ushort chanID = 0;
		foreach (FLChannel c in Channels)
		{
			c.Write(w, chanID++, chanFilter);
		}
		// For some reason, pattern colors go between
		for (int i = 0; i < Patterns.Count; i++)
		{
			Patterns[i].WriteColorAndNameIfNecessary(w, (ushort)(i + 1));
		}
		//
		chanFilter = 0;
		foreach (FLAutomation a in Automations)
		{
			a.Write(w, chanID++, chanFilter, PPQN);
		}
	}
	private void WriteArrangement(EndianBinaryWriter w, ushort arrangementIndex)
	{
		WriteWordEvent(w, FLEvent.NewArrangement, arrangementIndex);
		WriteUTF16EventWithLength(w, FLEvent.PlaylistArrangementName, "Arrangement\0");
		WriteByteEvent(w, FLEvent.Unk_36, 0);

		// Playlist Items

		// Must be in order of AbsoluteTick
		PlaylistItems.Sort((p1, p2) => p1.AbsoluteTick.CompareTo(p2.AbsoluteTick));

		w.WriteEnum(FLEvent.PlaylistItems);
		WriteTextEventLength(w, (uint)PlaylistItems.Count * FLPlaylistItem.LEN);
		foreach (FLPlaylistItem item in PlaylistItems)
		{
			item.Write(w, Patterns, Channels.Count, Automations, PlaylistTracks);
		}

		// Playlist Markers
		foreach (FLPlaylistMarker mark in PlaylistMarkers)
		{
			mark.Write(w);
		}

		// Playlist Tracks
		for (int i = 0; i < PlaylistTracks.Count; i++)
		{
			PlaylistTracks[i].Write(w, i);
		}
	}
	private static void WriteMoreStuffIDK(EndianBinaryWriter w)
	{
		WriteWordEvent(w, FLEvent.CurArrangementNum, 0);
		WriteByteEvent(w, FLEvent.APDC, 1);
		WriteByteEvent(w, FLEvent.Unk_39, 1);
		WriteByteEvent(w, FLEvent.ShouldCutNotesFast, 0);
		WriteByteEvent(w, FLEvent.EEAutoMode, 0);
		WriteByteEvent(w, FLEvent.Unk_38, 1);
	}
	private static void WriteInsertMaster(EndianBinaryWriter w)
	{
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_InsertMasterAndCurrent);

		WriteWordEvent(w, FLEvent.Unk_98, 0);
		WriteWordEvent(w, FLEvent.Unk_98, 1);
		WriteWordEvent(w, FLEvent.Unk_98, 2);
		WriteWordEvent(w, FLEvent.Unk_98, 3);
		WriteWordEvent(w, FLEvent.Unk_98, 4);
		WriteWordEvent(w, FLEvent.Unk_98, 5);
		WriteWordEvent(w, FLEvent.Unk_98, 6);
		WriteWordEvent(w, FLEvent.Unk_98, 7);
		WriteWordEvent(w, FLEvent.Unk_98, 8);
		WriteWordEvent(w, FLEvent.Unk_98, 9);
		w.WriteEnum(FLEvent.FXRouting); WriteTextEventLength(w, 127); w.WriteZeroes(127);
		WriteDWordEvent(w, FLEvent.Unk_165, 3);
		WriteDWordEvent(w, FLEvent.Unk_166, 1);
		WriteDWordEvent(w, FLEvent.FXInChanNum, uint.MaxValue);
		WriteDWordEvent(w, FLEvent.FXOutChanNum, 0);
	}
	private static void WriteInsert1(EndianBinaryWriter w)
	{
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_Insert1Through125);

		WriteUTF16EventWithLength(w, FLEvent.DefPluginName, "Fruity LSD\0");
		WriteBytesEventWithLength(w, FLEvent.NewPlugin, FruityLSD_NewPlugin);
		WriteDWordEvent(w, FLEvent.PluginIcon, 0);
		WriteDWordEvent(w, FLEvent.Color, 0x565148);
		WriteBytesEventWithLength(w, FLEvent.PluginParams, FruityLSD_PluginParams);

		WriteWordEvent(w, FLEvent.Unk_98, 0);
		WriteWordEvent(w, FLEvent.Unk_98, 1);
		WriteWordEvent(w, FLEvent.Unk_98, 2);
		WriteWordEvent(w, FLEvent.Unk_98, 3);
		WriteWordEvent(w, FLEvent.Unk_98, 4);
		WriteWordEvent(w, FLEvent.Unk_98, 5);
		WriteWordEvent(w, FLEvent.Unk_98, 6);
		WriteWordEvent(w, FLEvent.Unk_98, 7);
		WriteWordEvent(w, FLEvent.Unk_98, 8);
		WriteWordEvent(w, FLEvent.Unk_98, 9);
		w.WriteEnum(FLEvent.FXRouting); WriteTextEventLength(w, 127); w.WriteByte(1); w.WriteZeroes(126);
		WriteDWordEvent(w, FLEvent.Unk_165, 3);
		WriteDWordEvent(w, FLEvent.Unk_166, 1);
		WriteDWordEvent(w, FLEvent.FXInChanNum, uint.MaxValue);
		WriteDWordEvent(w, FLEvent.FXOutChanNum, uint.MaxValue);
	}
	private static void WriteInsert2Through125(EndianBinaryWriter w)
	{
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_Insert1Through125);

		WriteWordEvent(w, FLEvent.Unk_98, 0);
		WriteWordEvent(w, FLEvent.Unk_98, 1);
		WriteWordEvent(w, FLEvent.Unk_98, 2);
		WriteWordEvent(w, FLEvent.Unk_98, 3);
		WriteWordEvent(w, FLEvent.Unk_98, 4);
		WriteWordEvent(w, FLEvent.Unk_98, 5);
		WriteWordEvent(w, FLEvent.Unk_98, 6);
		WriteWordEvent(w, FLEvent.Unk_98, 7);
		WriteWordEvent(w, FLEvent.Unk_98, 8);
		WriteWordEvent(w, FLEvent.Unk_98, 9);
		w.WriteEnum(FLEvent.FXRouting); WriteTextEventLength(w, 127); w.WriteByte(1); w.WriteZeroes(126);
		WriteDWordEvent(w, FLEvent.Unk_165, 3);
		WriteDWordEvent(w, FLEvent.Unk_166, 1);
		WriteDWordEvent(w, FLEvent.FXInChanNum, uint.MaxValue);
		WriteDWordEvent(w, FLEvent.FXOutChanNum, uint.MaxValue);
	}
	private static void WriteInsertCurrent(EndianBinaryWriter w)
	{
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_InsertMasterAndCurrent);

		WriteWordEvent(w, FLEvent.Unk_98, 0);
		WriteWordEvent(w, FLEvent.Unk_98, 1);
		WriteWordEvent(w, FLEvent.Unk_98, 2);
		WriteWordEvent(w, FLEvent.Unk_98, 3);
		WriteWordEvent(w, FLEvent.Unk_98, 4);
		WriteWordEvent(w, FLEvent.Unk_98, 5);
		WriteWordEvent(w, FLEvent.Unk_98, 6);
		WriteWordEvent(w, FLEvent.Unk_98, 7);
		WriteWordEvent(w, FLEvent.Unk_98, 8);
		WriteWordEvent(w, FLEvent.Unk_98, 9);
		w.WriteEnum(FLEvent.FXRouting); WriteTextEventLength(w, 127); w.WriteZeroes(127);
		WriteDWordEvent(w, FLEvent.Unk_165, 3);
		WriteDWordEvent(w, FLEvent.Unk_166, 1);
		WriteDWordEvent(w, FLEvent.FXInChanNum, uint.MaxValue);
		WriteDWordEvent(w, FLEvent.FXOutChanNum, uint.MaxValue);
	}
	private static void WriteEnding(EndianBinaryWriter w)
	{
		WriteBytesEventWithLength(w, FLEvent.InitCtrlRecChan, _initCtrlRecChan);
		WriteDWordEvent(w, FLEvent.WindowH, 1286);
	}
}
