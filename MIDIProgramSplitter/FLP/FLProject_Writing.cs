using Kermalis.EndianBinaryIO;
using System;
using System.IO;
using System.Text;

namespace MIDIProgramSplitter.FLP;

partial class FLProject
{
	/// <summary>The project was started on 26/4/23 17:20. Total time spent on it: 4 minutes? This is probably the wrong file</summary>
	private static ReadOnlySpan<byte> ProjectTime_Default => new byte[16] { 0xC5, 0x8D, 0x96, 0x1D, 0x57, 0xFE, 0xE5, 0x40, 0x00, 0x00, 0x00, 0x50, 0xAA, 0x93, 0x25, 0x3F };

	private static ReadOnlySpan<byte> RemoteCtrl_MIDI0 => new byte[20] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x90, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0xD5, 0x01, 0x00, 0x00 };
	private static ReadOnlySpan<byte> RemoteCtrl_MIDI1 => new byte[20] { 0xFD, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x90, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0xD5, 0x01, 0x00, 0x00 };
	private static ReadOnlySpan<byte> RemoteCtrl_MIDI2 => new byte[20] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0xFF, 0x0F, 0x04, 0x00, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0xFF };
	private static ReadOnlySpan<byte> RemoteCtrl_Int => new byte[20] { 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0xD5, 0x01, 0x00, 0x00 };

	// Probably the border between inserts. 0x44 if Insert1 is muted. 0x4C 0x04 if unmuted and separator
	private static ReadOnlySpan<byte> FXParams_Insert0And125 => new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
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

	private static readonly byte[] _initCtrlRecChan = File.ReadAllBytes("../../../../InitCtrlRecChan.bin");

	public static void Write(Stream s, ReadOnlySpan<FLChannel> channels, ReadOnlySpan<FLAutomation> automations, ReadOnlySpan<FLPattern> patterns, ReadOnlySpan<FLPlaylistItem> playlistItems,
		ushort ppqn = 96 / 4)
	{
		var w = new EndianBinaryWriter(s, ascii: true);

		w.WriteChars("FLhd");
		w.WriteUInt32(6);
		w.WriteUInt16(0);
		w.WriteUInt16((ushort)channels.Length);
		w.WriteUInt16(ppqn);
		w.WriteChars("FLdt");

		long dataLenOffset = s.Position;
		w.WriteUInt32(0); // Write length later

		WriteProjectInfo(w, automations, patterns);
		uint chanFilter = automations.Length == 0 ? 0u : 1;
		for (int i = 0; i < channels.Length; i++)
		{
			channels[i].Write(w, i, chanFilter);
		}
		WriteNonsense(w, playlistItems);
		WriteInsertMaster(w);
		WriteInsert1(w);
		for (int i = 0; i < 125; i++)
		{
			WriteInsert2Through124(w);
		}
		WriteEnding(w);

		// Write length
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

	private static void WriteProjectInfo(EndianBinaryWriter w, ReadOnlySpan<FLAutomation> automations, ReadOnlySpan<FLPattern> patterns)
	{
		WriteUTF8EventWithLength(w, FLEvent.Version, "20.9.2.2963\0");
		WriteDWordEvent(w, FLEvent.Unk_159, 2963);
		WriteByteEvent(w, FLEvent.IsRegistered, 1);
		WriteByteEvent(w, FLEvent.Unk_37, 1);
		WriteUTF16EventWithLength(w, FLEvent.RegistrationID, "d3@?4xufs49p1n?B>;?889\0");
		WriteDWordEvent(w, FLEvent.FineTempo, 140_000); // 140 BPM
		WriteWordEvent(w, FLEvent.CurrentPatNum, 1);
		WriteByteEvent(w, FLEvent.LoopActive, 1);
		WriteByteEvent(w, FLEvent.Shuffle, 0);
		WriteWordEvent(w, FLEvent.MainPitch, 0);
		WriteByteEvent(w, FLEvent.TimeSigNumerator, 4);
		WriteByteEvent(w, FLEvent.TimeSigDenominator, 4);
		WriteByteEvent(w, FLEvent.Unk_35, 1);
		WriteByteEvent(w, FLEvent.PanVolumeTab, 0);
		WriteByteEvent(w, FLEvent.TruncateClipNotes, 1);
		WriteByteEvent(w, FLEvent.ShowInfo, 0);
		WriteUTF16EventWithLength(w, FLEvent.Title, "\0");
		WriteUTF16EventWithLength(w, FLEvent.Genre, "\0");
		WriteUTF16EventWithLength(w, FLEvent.Author, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectDataPath, "\0");
		WriteUTF16EventWithLength(w, FLEvent.Comment, "\0");
		WriteBytesEventWithLength(w, FLEvent.ProjectTime, ProjectTime_Default);
		if (automations.Length == 0)
		{
			WriteUTF16EventWithLength(w, FLEvent.ChanGroupName, "Unsorted\0");
			WriteDWordEvent(w, FLEvent.CurFilterNum, 0);
		}
		else
		{
			WriteUTF16EventWithLength(w, FLEvent.ChanGroupName, "Automation\0");
			WriteUTF16EventWithLength(w, FLEvent.ChanGroupName, "Unsorted\0");
			WriteDWordEvent(w, FLEvent.CurFilterNum, 1);
		}
		if (patterns.Length == 0)
		{
			WriteBytesEventWithLength(w, FLEvent.CtrlRecChan, Array.Empty<byte>());
		}
		else
		{
			for (int i = 0; i < patterns.Length; i++)
			{
				WriteWordEvent(w, FLEvent.NewPat, (ushort)(i + 1));
				patterns[i].WritePatternNotes(w);
			}
		}
		WriteBytesEventWithLength(w, FLEvent.RemoteCtrl_MIDI, RemoteCtrl_MIDI0);
		WriteBytesEventWithLength(w, FLEvent.RemoteCtrl_MIDI, RemoteCtrl_MIDI1);
		WriteBytesEventWithLength(w, FLEvent.RemoteCtrl_MIDI, RemoteCtrl_MIDI2);
		if (automations.Length > 0)
		{
			WriteBytesEventWithLength(w, FLEvent.RemoteCtrl_Int, RemoteCtrl_Int);
		}
	}
	private static void WriteNonsense(EndianBinaryWriter w, ReadOnlySpan<FLPlaylistItem> playlistItems)
	{
		WriteWordEvent(w, FLEvent.Unk_99, 0);
		WriteUTF16EventWithLength(w, FLEvent.Unk_241, "Arrangement\0");
		WriteByteEvent(w, FLEvent.Unk_36, 0);
		w.WriteEnum(FLEvent.PlayListItems);
		WriteTextEventLength(w, (uint)playlistItems.Length * FLPlaylistItem.LEN);
		foreach (FLPlaylistItem item in playlistItems)
		{
			item.Write(w);
		}
		for (int i = 0; i < 500; i++)
		{
			FLPlaylistTrack.Write(w, i);
		}
		WriteWordEvent(w, FLEvent.Unk_100, 0);
		WriteByteEvent(w, FLEvent.APDC, 1);
		WriteByteEvent(w, FLEvent.Unk_39, 1);
		WriteByteEvent(w, FLEvent.Unk_40, 0);
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
