using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MIDIProgramSplitter.FLP;

public sealed class FLProjectWriter
{
	/// <summary>The project was started on 26/4/23 17:20. Total time spent on it: 4 minutes? This is probably the wrong file</summary>
	private static ReadOnlySpan<byte> ProjectTime_Default => new byte[16] { 0xC5, 0x8D, 0x96, 0x1D, 0x57, 0xFE, 0xE5, 0x40, 0x00, 0x00, 0x00, 0x50, 0xAA, 0x93, 0x25, 0x3F };

	private static ReadOnlySpan<byte> MIDIInfo0 => new byte[20]
	{
		0x01, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x01,
		0x90, // 144
		0xFF, 0x0F,
		0x04, 0x00, 0x00, 0x00,
		0xD5, 0x01, // 469
		0x00, 0x00
	};
	private static ReadOnlySpan<byte> MIDIInfo1 => new byte[20]
	{
		0xFD, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x80, // 128
		0x90, // 144
		0xFF, 0x0F,
		0x04, 0x00, 0x00, 0x00,
		0xD5, 0x01, // 469
		0x00, 0x00
	};
	private static ReadOnlySpan<byte> MIDIInfo2 => new byte[20]
	{
		0xFF, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x04, // 4
		0x00,
		0xFF, 0x0F,
		0x04, 0x00, 0x00, 0x00,
		0x00, 0xFE,
		0xFF, 0xFF
	};

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

	public FLProjectWriter()
	{
		// FL Default
		PPQN = 96;
		CurrentTempo = 140;
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

		WriteHeaderChunk(w);
		WriteDataChunk(w);
	}
	private void WriteHeaderChunk(EndianBinaryWriter w)
	{
		w.WriteChars("FLhd");
		w.WriteUInt32(6); // Length
		w.WriteUInt16(0); // Format
		w.WriteUInt16((ushort)(Channels.Count + Automations.Count));
		w.WriteUInt16(PPQN);
	}
	private void WriteDataChunk(EndianBinaryWriter w)
	{
		w.WriteChars("FLdt");

		long dataLenOffset = w.Stream.Position;
		w.WriteUInt32(0); // Write length later

		WriteProjectInfo(w);
		WriteChannels(w);
		WriteArrangement(w, 0);
		WriteMoreStuffIDK(w);
		WriteMixer(w);

		// Write chunk length
		uint length = (uint)(w.Stream.Length - (dataLenOffset + 4));
		w.Stream.Position = dataLenOffset;
		w.WriteUInt32(length);
	}

	internal static void Write8BitEvent(EndianBinaryWriter w, FLEvent ev, byte data)
	{
		w.WriteEnum(ev);
		w.WriteByte(data);
	}
	internal static void Write16BitEvent(EndianBinaryWriter w, FLEvent ev, ushort data)
	{
		w.WriteEnum(ev);
		w.WriteUInt16(data);
	}
	internal static void Write32BitEvent(EndianBinaryWriter w, FLEvent ev, uint data)
	{
		w.WriteEnum(ev);
		w.WriteUInt32(data);
	}
	internal static void WriteArrayEventLength(EndianBinaryWriter w, uint length)
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
	internal static void WriteUTF8EventWithLength(EndianBinaryWriter w, FLEvent ev, string str)
	{
		WriteArrayEventWithLength(w, ev, Encoding.UTF8.GetBytes(str));
	}
	internal static void WriteUTF16EventWithLength(EndianBinaryWriter w, FLEvent ev, string str)
	{
		WriteArrayEventWithLength(w, ev, Encoding.Unicode.GetBytes(str));
	}
	internal static void WriteArrayEventWithLength(EndianBinaryWriter w, FLEvent ev, ReadOnlySpan<byte> bytes)
	{
		w.WriteEnum(ev);
		WriteArrayEventLength(w, (uint)bytes.Length);
		w.WriteBytes(bytes);
	}

	private void WriteProjectInfo(EndianBinaryWriter w)
	{
		WriteUTF8EventWithLength(w, FLEvent.Version, "20.9.2.2963\0");
		Write32BitEvent(w, FLEvent.VersionBuildNumber, 2963);
		Write8BitEvent(w, FLEvent.IsRegistered, 1); // TODO: Probably should make it 0 to avoid legal trouble? How does it authenticate this?
		Write8BitEvent(w, FLEvent.Unk_37, 1); // Authentication related too?
		WriteUTF16EventWithLength(w, FLEvent.RegistrationID, "d3@?4xufs49p1n?B>;?889\0"); // Probably shouldn't include this?

		Write32BitEvent(w, FLEvent.FineTempo, (uint)(CurrentTempo * 1_000));
		Write16BitEvent(w, FLEvent.SelectedPatternNum, 1);
		Write8BitEvent(w, FLEvent.IsSongMode, 1);
		Write8BitEvent(w, FLEvent.Shuffle, 0);
		Write16BitEvent(w, FLEvent.MasterPitch, 0);
		Write8BitEvent(w, FLEvent.ProjectTimeSigNumerator, TimeSigNumerator);
		Write8BitEvent(w, FLEvent.ProjectTimeSigDenominator, TimeSigDenominator);
		Write8BitEvent(w, FLEvent.ProjectShouldUseTimeSignatures, 1);
		Write8BitEvent(w, FLEvent.PanningLaw, 0); // Circular
		Write8BitEvent(w, FLEvent.ShouldPlayTruncatedClipNotes, 1);
		Write8BitEvent(w, FLEvent.ShouldShowInfoOnOpen, 0);
		WriteUTF16EventWithLength(w, FLEvent.ProjectTitle, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectGenre, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectAuthor, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectDataPath, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectComment, "\0");
		// ProjectURL would go here
		WriteArrayEventWithLength(w, FLEvent.ProjectTime, ProjectTime_Default);

		WriteChanFilters(w);
		WritePatterns(w);

		// No idea what these mean, but there are 3 and they're always the same in all my projects.
		// Dunno if it's MIDI keyboard related or something, because I only have 1
		WriteArrayEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo0);
		WriteArrayEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo1);
		WriteArrayEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo2);
	}
	private void WriteChanFilters(EndianBinaryWriter w)
	{
		// TODO: object. Also, have an option to split tracks into separate chan filters (would help a lot for complex songs)
		if (Automations.Count > 0)
		{
			WriteUTF16EventWithLength(w, FLEvent.ChanFilterName, "Automation\0");
			WriteUTF16EventWithLength(w, FLEvent.ChanFilterName, "Unsorted\0");
			Write32BitEvent(w, FLEvent.CurFilterNum, 1);
		}
		else
		{
			WriteUTF16EventWithLength(w, FLEvent.ChanFilterName, "Unsorted\0");
			Write32BitEvent(w, FLEvent.CurFilterNum, 0);
		}
	}
	private void WritePatterns(EndianBinaryWriter w)
	{
		if (Patterns.Count == 0)
		{
			WriteArrayEventWithLength(w, FLEvent.CtrlRecChan, Array.Empty<byte>());
		}
		else
		{
			// TODO: Why did this CtrlRecChan only show up sometimes?
			// Bytes: CtrlRecChan - 12 = [0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00]
			WriteArrayEventWithLength(w, FLEvent.CtrlRecChan, Array.Empty<byte>()); // This appeared when resizing a pattern without changing it
			for (int i = 0; i < Patterns.Count; i++)
			{
				Write16BitEvent(w, FLEvent.NewPattern, (ushort)(i + 1));
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
		Write16BitEvent(w, FLEvent.NewArrangement, arrangementIndex);
		WriteUTF16EventWithLength(w, FLEvent.PlaylistArrangementName, "Arrangement\0");
		Write8BitEvent(w, FLEvent.Unk_36, 0);

		// Playlist Items

		// Must be in order of AbsoluteTick
		PlaylistItems.Sort((p1, p2) => p1.AbsoluteTick.CompareTo(p2.AbsoluteTick));

		w.WriteEnum(FLEvent.PlaylistItems);
		WriteArrayEventLength(w, (uint)PlaylistItems.Count * FLPlaylistItem.LEN);
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
		Write16BitEvent(w, FLEvent.CurArrangementNum, 0);
		Write8BitEvent(w, FLEvent.APDC, 1);
		Write8BitEvent(w, FLEvent.Unk_39, 1);
		Write8BitEvent(w, FLEvent.ShouldCutNotesFast, 0);
		Write8BitEvent(w, FLEvent.EEAutoMode, 0);
		Write8BitEvent(w, FLEvent.Unk_38, 1);
	}

	private static void WriteMixer(EndianBinaryWriter w)
	{
		WriteInsertMaster(w); // 0
		WriteInsert1(w); // Different because of Fruity LSD
		for (int i = 2; i <= 125; i++)
		{
			WriteInsert2Through125(w);
		}
		WriteInsertCurrent(w); // 126

		FLMixerParams.Write(w);

		Write32BitEvent(w, FLEvent.WindowH, 1286);
	}
	private static void WriteInsertMaster(EndianBinaryWriter w)
	{
		FLInsertParams.Write(w, true);

		Write16BitEvent(w, FLEvent.NewInsertSlot, 0);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 1);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 2);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 3);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 4);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 5);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 6);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 7);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 8);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 9);
		// bool[127]. Go to nothing
		w.WriteEnum(FLEvent.InsertRouting); WriteArrayEventLength(w, 127); w.WriteZeroes(127);
		Write32BitEvent(w, FLEvent.Unk_165, 3);
		Write32BitEvent(w, FLEvent.Unk_166, 1);
		Write32BitEvent(w, FLEvent.InsertInChanNum, uint.MaxValue);
		Write32BitEvent(w, FLEvent.InsertOutChanNum, 0);
	}
	private static void WriteInsert1(EndianBinaryWriter w)
	{
		FLInsertParams.Write(w, false);

		WriteUTF16EventWithLength(w, FLEvent.DefPluginName, "Fruity LSD\0");
		WriteArrayEventWithLength(w, FLEvent.NewPlugin, FLNewPlugin.FruityLSD_NewPlugin);
		Write32BitEvent(w, FLEvent.PluginIcon, 0);
		Write32BitEvent(w, FLEvent.Color, 0x565148); // R 86, G 81, B 72
		WriteArrayEventWithLength(w, FLEvent.PluginParams, FLPluginParams.FruityLSD_PluginParams);

		Write16BitEvent(w, FLEvent.NewInsertSlot, 0);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 1);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 2);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 3);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 4);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 5);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 6);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 7);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 8);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 9);
		// bool[127]. Go to master and nothing else
		w.WriteEnum(FLEvent.InsertRouting); WriteArrayEventLength(w, 127); w.WriteByte(1); w.WriteZeroes(126);
		Write32BitEvent(w, FLEvent.Unk_165, 3);
		Write32BitEvent(w, FLEvent.Unk_166, 1);
		Write32BitEvent(w, FLEvent.InsertInChanNum, uint.MaxValue);
		Write32BitEvent(w, FLEvent.InsertOutChanNum, uint.MaxValue);
	}
	private static void WriteInsert2Through125(EndianBinaryWriter w)
	{
		FLInsertParams.Write(w, false);

		Write16BitEvent(w, FLEvent.NewInsertSlot, 0);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 1);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 2);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 3);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 4);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 5);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 6);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 7);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 8);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 9);
		// bool[127]. Go to master and nothing else
		w.WriteEnum(FLEvent.InsertRouting); WriteArrayEventLength(w, 127); w.WriteByte(1); w.WriteZeroes(126);
		Write32BitEvent(w, FLEvent.Unk_165, 3);
		Write32BitEvent(w, FLEvent.Unk_166, 1);
		Write32BitEvent(w, FLEvent.InsertInChanNum, uint.MaxValue);
		Write32BitEvent(w, FLEvent.InsertOutChanNum, uint.MaxValue);
	}
	private static void WriteInsertCurrent(EndianBinaryWriter w)
	{
		FLInsertParams.Write(w, true);

		Write16BitEvent(w, FLEvent.NewInsertSlot, 0);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 1);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 2);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 3);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 4);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 5);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 6);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 7);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 8);
		Write16BitEvent(w, FLEvent.NewInsertSlot, 9);
		// bool[127]. Go to nothing
		w.WriteEnum(FLEvent.InsertRouting); WriteArrayEventLength(w, 127); w.WriteZeroes(127);
		Write32BitEvent(w, FLEvent.Unk_165, 3);
		Write32BitEvent(w, FLEvent.Unk_166, 1);
		Write32BitEvent(w, FLEvent.InsertInChanNum, uint.MaxValue);
		Write32BitEvent(w, FLEvent.InsertOutChanNum, uint.MaxValue);
	}
}
