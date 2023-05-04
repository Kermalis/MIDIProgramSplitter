using Kermalis.EndianBinaryIO;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MIDIProgramSplitter.FLP;

public sealed class FLProjectReader
{
	public readonly ushort PPQN;
	public readonly StringBuilder Log;

	public FLProjectReader(Stream s)
	{
		var r = new EndianBinaryReader(s, ascii: true);

		ReadHeaderChunk(r, out PPQN);

		Log = new StringBuilder();

		ReadDataChunk(r);
	}
	private static void ReadHeaderChunk(EndianBinaryReader r, out ushort ppqn)
	{
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

		ppqn = r.ReadUInt16();
	}
	private void ReadDataChunk(EndianBinaryReader r)
	{
		Span<char> chars = stackalloc char[4];
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

		while (r.Stream.Position < r.Stream.Length)
		{
			LogPos(r.Stream.Position);

			FLEvent ev = r.ReadEnum<FLEvent>();
			uint data = ReadDataForEvent(r, ev, out byte[]? bytes);

			if (ev < FLEvent.NewChannel)
			{
				HandleEvent_8Bit(ev, data);
			}
			else if (ev is >= FLEvent.NewChannel and < FLEvent.Color)
			{
				HandleEvent_16Bit(ev, data);
			}
			else if (ev is >= FLEvent.Color and < FLEvent.ChannelName)
			{
				HandleEvent_32Bit(ev, data);
			}
			else // >= FLEvent.ChanName
			{
				HandleEvent_Array(ev, bytes!);
			}
		}
	}
	private static uint ReadDataForEvent(EndianBinaryReader r, FLEvent ev, out byte[]? bytes)
	{
		bytes = null;

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
			bytes = new byte[ReadArrayLen(r, data)];
			r.ReadBytes(bytes);
		}

		return data;
	}
	private void HandleEvent_8Bit(FLEvent ev, uint data)
	{
		switch (ev)
		{
			case FLEvent.ChannelType:
			{
				LogLine(string.Format("Byte: {0} = {1} ({2})", ev, data, (FLChannelType)data));
				break;
			}
			default:
			{
				CheckEventExists(ev);

				LogLine(string.Format("Byte: {0} = {1}", ev, data));
				break;
			}
		}
	}
	private void HandleEvent_16Bit(FLEvent ev, uint data)
	{
		switch (ev)
		{
			case FLEvent.Fade_Stereo:
			{
				LogLine(string.Format("Word: {0} = 0x{1:X} ({2})", ev, data, (FLFadeStereo)data));
				break;
			}
			case FLEvent.SwingMix:
			case FLEvent.FX:
			case FLEvent.FX3:
			case FLEvent.StDel:
			case FLEvent.CutOff:
			{
				LogLine(string.Format("Word: {0} = 0x{1:X}", ev, data));
				break;
			}
			default:
			{
				CheckEventExists(ev);

				LogLine(string.Format("Word: {0} = {1}", ev, data));
				break;
			}
		}
	}
	private void HandleEvent_32Bit(FLEvent ev, uint data)
	{
		switch (ev)
		{
			case FLEvent.Color:
			case FLEvent.PatternColor:
			case FLEvent.InsertColor:
			{
				LogLine(string.Format("DWord: {0} = 0x{1:X6} ({2})", ev, data, new FLColor3(data)));
				break;
			}
			case FLEvent.DelayReso:
			case FLEvent.Reverb:
			case FLEvent.FXSine:
			case FLEvent.CutCutBy:
			case FLEvent.ChannelLayerFlags:
			case FLEvent.ChannelSampleFlags:
			case FLEvent.InsertInChanNum:
			case FLEvent.InsertOutChanNum:
			case FLEvent.Unk_157:
			case FLEvent.Unk_158:
			case FLEvent.NewTimeMarker:
			{
				LogLine(string.Format("DWord: {0} = 0x{1:X}", ev, data));
				break;
			}
			default:
			{
				CheckEventExists(ev);

				LogLine(string.Format("DWord: {0} = {1}", ev, data));
				break;
			}
		}
	}
	private void HandleEvent_Array(FLEvent ev, byte[] bytes)
	{
		string type;
		string str;

		if (ev == FLEvent.AutomationData)
		{
			type = "Bytes";
			str = FLAutomation.ReadData(bytes);
		}
		else if (ev == FLEvent.MixerParams)
		{
			type = "Bytes";
			str = FLMixerParams.ReadData(bytes);
		}
		else
		{
			CheckEventExists(ev);

			if (IsUTF8(ev))
			{
				type = "UTF8";
				str = DecodeString(Encoding.UTF8, bytes);
			}
			else if (IsUTF16(ev))
			{
				type = "UTF16";
				str = DecodeString(Encoding.Unicode, bytes);
			}
			else
			{
				type = "Bytes";
				str = BytesString(bytes);
			}
		}
		LogLine(string.Format("{0}: {1} - {2} = {3}",
			type, ev, bytes.Length, str));
	}

	private void LogPos(long pos)
	{
		Log.Append($"@ 0x{pos:X}\t");
		if (pos < 0x1000)
		{
			Log.Append('\t');
		}
	}
	private void LogLine(string msg)
	{
		Log.AppendLine(msg);
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
			case FLEvent.PlaylistItem: // PlaylistItems now
			case FLEvent.MainResoCutOff:
			case FLEvent.SSNote:
			case FLEvent.PatternAutoMode:

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
			case FLEvent.InsertName:
			case FLEvent.TimeMarkerName:
			case FLEvent.ProjectGenre:
			case FLEvent.ProjectAuthor:
			case FLEvent.RemoteCtrlFormula:
			case FLEvent.ChanFilterName:
			case FLEvent.PlaylistTrackName:
			case FLEvent.PlaylistArrangementName:
			case FLEvent.PatternName:
				return true;
		}
		return false;
	}
	private void CheckEventExists(FLEvent ev)
	{
		if (!Enum.IsDefined(ev))
		{
			LogLine("!!!!!!!!!!!!!!! UNDEFINED EVENT " + ev + " !!!!!!!!!!!!!!!");
		}
	}

	private static uint ReadArrayLen(EndianBinaryReader r, uint curByte)
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
