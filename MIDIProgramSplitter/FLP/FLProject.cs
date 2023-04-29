using Kermalis.EndianBinaryIO;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MIDIProgramSplitter.FLP;

internal sealed partial class FLProject
{
	private readonly ushort _ppqn;

	private readonly StringBuilder _log;

	public FLProject(Stream s)
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

			if (ev < FLEvent.NewChan)
			{
				HandleEvent_Byte(ev, data);
			}
			else if (ev is >= FLEvent.NewChan and < FLEvent.Color)
			{
				HandleEvent_Word(ev, data);
			}
			else if (ev is >= FLEvent.Color and < FLEvent.ChanName)
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

		if (ev is >= FLEvent.NewChan and < FLEvent.ChanName)
		{
			data |= (uint)r.ReadByte() << 8;
		}
		if (ev is >= FLEvent.Color and < FLEvent.ChanName)
		{
			data |= (uint)r.ReadByte() << 16;
			data |= (uint)r.ReadByte() << 24;
		}
		if (ev >= FLEvent.ChanName)
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
			case FLEvent.ChanType:
			{
				Log($"Byte: ChanType = {data} ({(FLChanType)data})");
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
		CheckEventExists(ev);

		Log(string.Format("Word: {0} = {1}", ev, data));
	}
	private void HandleEvent_DWord(FLEvent ev, uint data)
	{
		CheckEventExists(ev);

		Log(string.Format("DWord: {0} = {1}", ev, data));
	}
	private void HandleEvent_Text(FLEvent ev, byte[] text)
	{
		CheckEventExists(ev);

		string type;
		string str;
		if (IsUTF8(ev))
		{
			type = "UTF8";
			str = $"\"{Encoding.UTF8.GetString(text).Replace("\0", "\\0")}\"";
		}
		else if (IsUTF16(ev))
		{
			type = "UTF16";
			str = $"\"{Encoding.Unicode.GetString(text).Replace("\0", "\\0")}\"";
		}
		else
		{
			type = "Bytes";
			str = BytesString(text);
		}
		Log(string.Format("{0}: {1} - {2} = {3}", type, ev, text.Length, str));
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
			case FLEvent.ChanVol:
			case FLEvent.ChanPan:
			case FLEvent.MainVol:
			case FLEvent.FitToSteps:
			case FLEvent.Pitchable:
			case FLEvent.DelayFlags:
			case FLEvent.NStepsShown:
			// Word
			case FLEvent.Tempo:
			case FLEvent.RandChan:
			case FLEvent.MixChan:
			case FLEvent.OldSongLoopPos:
			case FLEvent.TempoFine:
			// DWord
			case FLEvent.PlayListItem:
			case FLEvent.MainResoCutOff:
			case FLEvent.SSNote:
			case FLEvent.PatAutoMode:
			// Text
			case FLEvent.ChanName:
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
			case FLEvent.Title:
			case FLEvent.Comment:
			case FLEvent.URL:
			case FLEvent.RegistrationID:
			case FLEvent.DefPluginName:
			case FLEvent.ProjectDataPath:
			case FLEvent.PluginName:
			case FLEvent.FXName:
			case FLEvent.TimeMarker:
			case FLEvent.Genre:
			case FLEvent.Author:
			case FLEvent.RemoteCtrlFormula:
			case FLEvent.ChanGroupName:
			case FLEvent.PLTrackName:
			case FLEvent.Unk_241:
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
