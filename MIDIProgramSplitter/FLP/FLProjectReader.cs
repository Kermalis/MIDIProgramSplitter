using Kermalis.EndianBinaryIO;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLProjectReader
{
	private readonly ushort _ppqn;

	private readonly StringBuilder _log;

	public FLProjectReader(Stream s)
	{
		var r = new EndianBinaryReader(s, ascii: true);

		Span<char> chars = stackalloc char[4];
		r.ReadChars(chars);

		if (!chars.SequenceEqual("FLhd"))
		{
			throw new InvalidDataException();
		}

		uint headerLen = r.ReadUInt32();
		if (headerLen != 6)
		{
			throw new InvalidDataException();
		}

		ushort format = r.ReadUInt16();
		if (format != 0)
		{
			throw new InvalidDataException();
		}

		ushort numChannels = r.ReadUInt16();
		if (numChannels is < 1 or > 1000)
		{
			throw new InvalidDataException();
		}

		_ppqn = r.ReadUInt16();

		// Now data chunk
		r.ReadChars(chars);

		if (!chars.SequenceEqual("FLdt"))
		{
			throw new InvalidDataException();
		}

		uint dataLen = r.ReadUInt32();
		if (dataLen >= 0x10_000_000 || dataLen != r.Stream.Length - r.Stream.Position)
		{
			throw new InvalidDataException();
		}

		_log = new StringBuilder();

		ReadData(r);

#if DEBUG && WINDOWS
		Utils.Win_SetClipboardString(_log.ToString());
#endif
	}
	private void ReadData(EndianBinaryReader r)
	{
		while (r.Stream.Position < r.Stream.Length)
		{
			LogPos(r.Stream.Position);

			FLEvent ev = r.ReadEnum<FLEvent>();
			uint data = ReadDataForEvent(r, ev, out byte[]? text);

			if (ev < FLEvent.NewChannel)
			{
				HandleEvent_Byte(ev, data);
			}
			else if (ev is >= FLEvent.NewChannel and < FLEvent.Color)
			{
				HandleEvent_Word(ev, data);
			}
			else if (ev is >= FLEvent.Color and < FLEvent.ChannelName)
			{
				HandleEvent_DWord(ev, data);
			}
			else // >= FLEvent.ChanName
			{
				HandleEvent_Text(ev, text!);
			}
		}
	}
	private static uint ReadDataForEvent(EndianBinaryReader r, FLEvent ev, out byte[]? text)
	{
		text = null;

		uint data = r.ReadByte();

		if (ev is >= FLEvent.NewChannel and < FLEvent.ChannelName)
		{
			data |= (uint)r.ReadByte() << 8;
		}
		if (ev is >= FLEvent.Color and < FLEvent.ChannelName)
		{
			data |= (uint)r.ReadByte() << 16;
			data |= (uint)r.ReadByte() << 24;
		}
		if (ev >= FLEvent.ChannelName)
		{
			// Only have the first byte in data currently
			text = new byte[ReadTextLen(r, data)];
			r.ReadBytes(text);
		}

		return data;
	}
	private void HandleEvent_Byte(FLEvent ev, uint data)
	{
		switch (ev)
		{
			case FLEvent.ChannelType:
			{
				Log(string.Format("Byte: {0} = {1} ({2})", ev, data, (FLChanType)data));
				break;
			}
			default:
			{
				CheckEventExists(ev);

				Log(string.Format("Byte: {0} = {1}", ev, data));
				break;
			}
		}
	}
	private void HandleEvent_Word(FLEvent ev, uint data)
	{
		switch (ev)
		{
			case FLEvent.SwingMix:
			case FLEvent.FX:
			case FLEvent.FX3:
			case FLEvent.StDel:
			case FLEvent.CutOff:
			{
				Log(string.Format("Word: {0} = 0x{1:X}", ev, data));
				break;
			}
			default:
			{
				CheckEventExists(ev);

				Log(string.Format("Word: {0} = {1}", ev, data));
				break;
			}
		}
	}
	private void HandleEvent_DWord(FLEvent ev, uint data)
	{
		switch (ev)
		{
			case FLEvent.Color:
			case FLEvent.PatColor:
			{
				Log(string.Format("DWord: {0} = 0x{1:X6} ({2})", ev, data, new FLColor3(data)));
				break;
			}
			case FLEvent.DelayReso:
			case FLEvent.Reverb:
			case FLEvent.FXSine:
			case FLEvent.CutCutBy:
			case FLEvent.ChannelLayerFlags:
			case FLEvent.ChannelSampleFlags:
			case FLEvent.FXInChanNum:
			case FLEvent.FXOutChanNum:
			case FLEvent.Unk_157:
			case FLEvent.Unk_158:
			case FLEvent.NewTimeMarker:
			{
				Log(string.Format("DWord: {0} = 0x{1:X}", ev, data));
				break;
			}
			default:
			{
				CheckEventExists(ev);

				Log(string.Format("DWord: {0} = {1}", ev, data));
				break;
			}
		}
	}
	private void HandleEvent_Text(FLEvent ev, byte[] text)
	{
		string type;
		string str;

		if (ev == FLEvent.AutomationData)
		{
			type = "Bytes";
			str = FLAutomation.ReadData(text);
		}
		else
		{
			CheckEventExists(ev);

			if (IsUTF8(ev))
			{
				type = "UTF8";
				str = DecodeString(Encoding.UTF8, text);
			}
			else if (IsUTF16(ev))
			{
				type = "UTF16";
				str = DecodeString(Encoding.Unicode, text);
			}
			else
			{
				type = "Bytes";
				str = BytesString(text);
			}
		}
		Log(string.Format("{0}: {1} - {2} = {3}",
			type, ev, text.Length, str));
	}

	private void CheckEventExists(FLEvent ev)
	{
		if (!Enum.IsDefined(ev))
		{
			_log.AppendLine("!!!!!!!!!!!!!!! UNDEFINED EVENT " + ev + " !!!!!!!!!!!!!!!");
		}
	}
	private void LogPos(long pos)
	{
		_log.Append($"@ 0x{pos:X}\t");
		if (pos < 0x1000)
		{
			_log.Append('\t');
		}
	}
	private void Log(string msg)
	{
		_log.AppendLine(msg);
	}
	private static string DecodeString(Encoding e, byte[] bytes)
	{
		string str = e.GetString(bytes)
			.Replace("\0", "\\0")
			.Replace("\r", "\\r")
			.Replace("\n", "\\n");
		return '\"' + str + '\"';
	}
	private static string BytesString(byte[] bytes)
	{
		if (bytes.Length == 0)
		{
			return "[]";
		}
		return "[0x" + string.Join(", 0x", bytes.Select(b => b.ToString("X2"))) + ']';
	}
	// https://github.com/jdstmporter/FLPFiles/tree/main/src/FLP/messagetypes
	private static bool IsObsolete(FLEvent ev)
	{
		switch (ev)
		{
			// Byte
			case FLEvent.ChannelVolume:
			case FLEvent.ChannelPanpot:
			case FLEvent.MainVolume: // Now stored in _initCtrlRecChan
			case FLEvent.FitToSteps:
			case FLEvent.Pitchable:
			case FLEvent.DelayFlags:
			case FLEvent.NStepsShown:

			// Word
			case FLEvent.Tempo: // FineTempo is used now
			case FLEvent.RandChan:
			case FLEvent.MixChan:
			case FLEvent.OldSongLoopPos:
			case FLEvent.TempoFine: // FineTempo is used now

			// DWord
			case FLEvent.PlaylistItem:
			case FLEvent.MainResoCutOff:
			case FLEvent.SSNote:
			case FLEvent.PatAutoMode:

			// Text
			case FLEvent.ChannelName: // PluginName is used now
			case FLEvent.DelayLine:
			case FLEvent.OldFilterParams:
				return true;
		}
		return false;
	}
	private static bool IsUTF8(FLEvent ev)
	{
		switch (ev)
		{
			case FLEvent.Version:
				return true;
		}
		return false;
	}
	private static bool IsUTF16(FLEvent ev)
	{
		switch (ev)
		{
			case FLEvent.ProjectTitle:
			case FLEvent.ProjectComment:
			case FLEvent.ProjectURL:
			case FLEvent.RegistrationID:
			case FLEvent.DefPluginName:
			case FLEvent.ProjectDataPath:
			case FLEvent.PluginName:
			case FLEvent.FXName:
			case FLEvent.TimeMarkerName:
			case FLEvent.ProjectGenre:
			case FLEvent.ProjectAuthor:
			case FLEvent.RemoteCtrlFormula:
			case FLEvent.ChanFilterName:
			case FLEvent.PlaylistTrackName:
			case FLEvent.PlaylistArrangementName:
				return true;
		}
		return false;
	}

	private static uint ReadTextLen(EndianBinaryReader r, uint curByte)
	{
		// TODO: How many bytes can this len use?
		uint len = curByte & 0x7F;
		byte shift = 0;
		while ((curByte & 0x80) != 0)
		{
			shift += 7;
			curByte = r.ReadByte();
			len |= (curByte & 0x7F) << shift;
		}
		return len;
	}
}
