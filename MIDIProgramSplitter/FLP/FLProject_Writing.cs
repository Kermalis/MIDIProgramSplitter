﻿using Kermalis.EndianBinaryIO;
using System;
using System.IO;
using System.Text;

namespace MIDIProgramSplitter.FLP;

partial class FLProject
{
	/// <summary>The project was started on 26/4/23 17:20. Total time spent on it: 4 minutes? This is probably the wrong file</summary>
	private static ReadOnlySpan<byte> ProjectTime_Default => new byte[16] { 0xC5, 0x8D, 0x96, 0x1D, 0x57, 0xFE, 0xE5, 0x40, 0x00, 0x00, 0x00, 0x50, 0xAA, 0x93, 0x25, 0x3F };

	private static ReadOnlySpan<byte> MIDIInfo0 => new byte[20] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x90, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0xD5, 0x01, 0x00, 0x00 };
	private static ReadOnlySpan<byte> MIDIInfo1 => new byte[20] { 0xFD, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x90, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0xD5, 0x01, 0x00, 0x00 };
	private static ReadOnlySpan<byte> MIDIInfo2 => new byte[20] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0xFF };

	// 0x0C if unmuted, 0x04 if muted.
	private static ReadOnlySpan<byte> FXParams_Insert0And125 => new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	// 0x44 0x00 if Insert1 is muted. 0x4C 0x04 if unmuted and separator
	private static ReadOnlySpan<byte> FXParams_Insert1Through124 => new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x4C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

	private static ReadOnlySpan<byte> FruityLSD_PluginParams => new byte[97]
	{
		/* DokiDoki:
		0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x2D, 0x00, 0x00, 0x00, 0x49, 0x00, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00, 0x47, 0x00, 0x00,
		0x00, 0x44, 0x00, 0x00, 0x00, 0x70, 0x00, 0x00, 0x00, 0x7C, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00,
		0x00, 0x22, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x80, 0x01, 0x00, 0x00, 0x00, 0x4F, 0x00, 0x00,
		0x00, 0x4F, 0x00, 0x00, 0x00, 0x3B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00
		*/
		// Blank:
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

	public static void Write(Stream s, ReadOnlySpan<FLChannel> channels, ReadOnlySpan<FLAutomation> automations, ReadOnlySpan<FLPattern> patterns, ReadOnlySpan<FLPlaylistItem> playlistItems,
		ushort ppqn = 96)
	{
		var w = new EndianBinaryWriter(s, ascii: true);

		// Header chunk
		w.WriteChars("FLhd");
		w.WriteUInt32(6); // Length
		w.WriteUInt16(0); // Format
		w.WriteUInt16((ushort)(channels.Length + automations.Length));
		w.WriteUInt16(ppqn);

		// Data chunk
		w.WriteChars("FLdt");

		long dataLenOffset = s.Position;
		w.WriteUInt32(0); // Write length later

		WriteProjectInfo(w, automations.Length != 0, patterns);
		WriteChannels(w, ppqn, channels, automations);
		WriteArrangement(w, 0, playlistItems);
		WriteMoreStuffIDK(w);
		WriteInsertMaster(w);
		WriteInsert1(w);
		for (int i = 0; i < 125; i++)
		{
			WriteInsert2Through124(w);
		}
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

	private static void WriteProjectInfo(EndianBinaryWriter w, bool hasAutomations, ReadOnlySpan<FLPattern> patterns)
	{
		WriteUTF8EventWithLength(w, FLEvent.Version, "20.9.2.2963\0");
		WriteDWordEvent(w, FLEvent.VersionBuildNumber, 2963);
		WriteByteEvent(w, FLEvent.IsRegistered, 1); // TODO: Probably should make it 0 to avoid legal trouble? How does it authenticate this?
		WriteByteEvent(w, FLEvent.Unk_37, 1); // Authentication related too?
		WriteUTF16EventWithLength(w, FLEvent.RegistrationID, "d3@?4xufs49p1n?B>;?889\0"); // Probably shouldn't include this?

		WriteDWordEvent(w, FLEvent.FineTempo, 140_000); // 140 BPM
		WriteWordEvent(w, FLEvent.CurPatternNum, 1);
		WriteByteEvent(w, FLEvent.IsSongMode, 1);
		WriteByteEvent(w, FLEvent.Shuffle, 0);
		WriteWordEvent(w, FLEvent.MasterPitch, 0);
		WriteByteEvent(w, FLEvent.TimeSigNumerator, 4);
		WriteByteEvent(w, FLEvent.TimeSigDenominator, 4);
		WriteByteEvent(w, FLEvent.ShouldUseTimeSignatures, 1);
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

		WriteChanFilters(w, hasAutomations);
		WritePatterns(w, patterns);

		// No idea what these mean, but there are 3 and they're always the same in all my projects.
		// Dunno if it's MIDI keyboard related or something, because I only have 1
		WriteBytesEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo0);
		WriteBytesEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo1);
		WriteBytesEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo2);
	}
	private static void WriteChanFilters(EndianBinaryWriter w, bool hasAutomations)
	{
		if (hasAutomations)
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
	private static void WritePatterns(EndianBinaryWriter w, ReadOnlySpan<FLPattern> patterns)
	{
		if (patterns.Length == 0)
		{
			WriteBytesEventWithLength(w, FLEvent.CtrlRecChan, Array.Empty<byte>());
		}
		else
		{
			// TODO: Why did this CtrlRecChan only show up sometimes?
			// Bytes: CtrlRecChan - 12 = [0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00]
			for (int i = 0; i < patterns.Length; i++)
			{
				WriteWordEvent(w, FLEvent.NewPattern, (ushort)(i + 1));
				patterns[i].WritePatternNotes(w);
			}
		}
	}
	private static void WriteChannels(EndianBinaryWriter w, uint ppqn, ReadOnlySpan<FLChannel> channels, ReadOnlySpan<FLAutomation> automations)
	{
		for (int i = 0; i < automations.Length; i++)
		{
			automations[i].WriteAutomationConnection(w, (ushort)(channels.Length + i), channels);
		}

		uint chanFilter = automations.Length == 0 ? 0u : 1;
		ushort chanID = 0;
		foreach (FLChannel c in channels)
		{
			c.Write(w, chanID++, chanFilter);
		}
		chanFilter = 0;
		foreach (FLAutomation a in automations)
		{
			a.Write(w, chanID++, chanFilter, ppqn);
		}
	}
	private static void WriteArrangement(EndianBinaryWriter w, ushort arrangementIndex, ReadOnlySpan<FLPlaylistItem> playlistItems)
	{
		WriteWordEvent(w, FLEvent.NewArrangement, arrangementIndex);
		WriteUTF16EventWithLength(w, FLEvent.PlaylistArrangementName, "Arrangement\0");
		WriteByteEvent(w, FLEvent.Unk_36, 0);

		// Playlist Items
		w.WriteEnum(FLEvent.PlaylistItems);
		WriteTextEventLength(w, (uint)playlistItems.Length * FLPlaylistItem.LEN);
		foreach (FLPlaylistItem item in playlistItems)
		{
			item.Write(w);
		}

		// Playlist Tracks
		for (int i = 0; i < 500; i++)
		{
			FLPlaylistTrack.Write(w, i);
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
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_Insert0And125);

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
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_Insert1Through124);

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
	private static void WriteInsert2Through124(EndianBinaryWriter w)
	{
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_Insert1Through124);

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
	private static void WriteInsert125(EndianBinaryWriter w)
	{
		WriteBytesEventWithLength(w, FLEvent.FXParams, FXParams_Insert0And125);

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
