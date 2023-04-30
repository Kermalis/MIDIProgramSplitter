using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLAutomation
{
	public struct Point
	{
		public const int LEN = 24;

		public uint AbsoluteTicks;
		public uint Value;
	}

	private static ReadOnlySpan<byte> NewPlugin_DeselectedTopLeft => new byte[52]
	{
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
		0x54, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x06, 0x00,
		0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00
	};

	// Contains min/max values
	private static ReadOnlySpan<byte> BasicChanParams => new byte[24] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	private static ReadOnlySpan<byte> ChanPoly => new byte[9] { 0x01, 0x00, 0x00, 0x00, 0xF4, 0x01, 0x00, 0x00, 0x00 };

	private static ReadOnlySpan<byte> ChanParams => new byte[168]
	{
		0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF,
		0x3C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F,
		0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
		0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x04, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00,
		0xA7, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x02, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x01, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x3F
	};

	public readonly string Name;
	public readonly List<Point> Points;

	public FLAutomation(string name)
	{
		Name = name;
		Points = new List<Point>();
	}

	// TODO: How does it distinguish a panpot/volume/voice/whatever automation? Luckily, it's NOT stored in that massive block at the end of the file
	public void Write(EndianBinaryWriter w, int i, uint filterNum)
	{
		FLProject.WriteWordEvent(w, FLEvent.NewChannel, (ushort)i);
		FLProject.WriteByteEvent(w, FLEvent.ChannelType, (byte)FLChanType.Automation);
		FLProject.WriteUTF16EventWithLength(w, FLEvent.DefPluginName, "\0");
		FLProject.WriteBytesEventWithLength(w, FLEvent.NewPlugin, NewPlugin_DeselectedTopLeft);
		FLProject.WriteUTF16EventWithLength(w, FLEvent.PluginName, Name + '\0');
		FLProject.WriteDWordEvent(w, FLEvent.PluginIcon, 0);
		FLProject.WriteDWordEvent(w, FLEvent.Color, 0x60608E);
		FLProject.WriteByteEvent(w, FLEvent.ChannelIsEnabled, 1);
		FLProject.WriteBytesEventWithLength(w, FLEvent.Delay, FLChannel.Delay);
		FLProject.WriteDWordEvent(w, FLEvent.DelayReso, 0x800_080);
		FLProject.WriteDWordEvent(w, FLEvent.Reverb, 0x10_000);
		FLProject.WriteWordEvent(w, FLEvent.ShiftDelay, 0);
		FLProject.WriteWordEvent(w, FLEvent.SwingMix, 0x80);
		FLProject.WriteWordEvent(w, FLEvent.FX, 0x80);
		FLProject.WriteWordEvent(w, FLEvent.FX3, 0x100);
		FLProject.WriteWordEvent(w, FLEvent.CutOff, 0x400);
		FLProject.WriteWordEvent(w, FLEvent.Resonance, 0);
		FLProject.WriteWordEvent(w, FLEvent.PreAmp, 0);
		FLProject.WriteWordEvent(w, FLEvent.Decay, 0);
		FLProject.WriteWordEvent(w, FLEvent.Attack, 0);
		FLProject.WriteWordEvent(w, FLEvent.StDel, 0x800);
		FLProject.WriteDWordEvent(w, FLEvent.FXSine, 0x800_000);
		FLProject.WriteWordEvent(w, FLEvent.Fade_Stereo, 0);
		FLProject.WriteByteEvent(w, FLEvent.TargetFXTrack, 0);
		FLProject.WriteBytesEventWithLength(w, FLEvent.BasicChanParams, BasicChanParams);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChanOfsLevels, FLChannel.ChanOfsLevels);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChanPoly, ChanPoly);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelParams, ChanParams);
		FLProject.WriteDWordEvent(w, FLEvent.CutCutBy, 0);
		FLProject.WriteDWordEvent(w, FLEvent.ChannelLayerFlags, 0);
		FLProject.WriteDWordEvent(w, FLEvent.ChanFilterNum, filterNum);
		WriteChanAC(w);
		FLProject.WriteByteEvent(w, FLEvent.Unk_32, 0);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelTracking, FLChannel.Tracking0);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelTracking, FLChannel.Tracking1);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.Envelope1);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProject.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProject.WriteDWordEvent(w, FLEvent.ChannelSampleFlags, 0b0011);
		FLProject.WriteByteEvent(w, FLEvent.ChannelLoopType, 0);
	}
	private void WriteChanAC(EndianBinaryWriter w)
	{

	}

	public static string ReadChanAC(byte[] data)
	{
		using (var ms = new MemoryStream(data))
		{
			var r = new EndianBinaryReader(ms);
			var str = new StringBuilder();

			void WriteLoc(int numBytes)
			{
				long i = ms.Position;
				str.AppendLine(string.Format(" // {0:D3}-{1:D3}", i, i + numBytes - 1));
			}
			void WriteLocE(int numBytes, string extra)
			{
				long i = ms.Position;
				str.AppendLine(string.Format(" // {0:D3}-{1:D3}: {2}", i, i + numBytes - 1, extra));
			}
			void WriteLocE1(string extra)
			{
				long i = ms.Position;
				str.AppendLine(string.Format(" // {0:D3}: {1}", i, extra));
			}

			str.AppendLine("{");

			WriteLocE(4, "Always 1?");
			str.AppendLine($" {r.ReadUInt32()},");

			WriteLocE(4, "Always 0x40 (64)?");
			str.AppendLine($" 0x{r.ReadUInt32():X},");

			str.AppendLine();
			WriteLocE1("0 for vol, 1 for pan");
			str.AppendLine($" 0x{r.ReadByte():X2},");

			str.AppendLine();
			WriteLocE(4, "Always 4?");
			str.AppendLine($" {r.ReadUInt32()},");
			WriteLocE(4, "Always 3?");
			str.AppendLine($" {r.ReadUInt32()},");

			str.AppendLine();
			WriteLocE(4, "Num points");
			str.AppendLine($" {r.ReadUInt32()},");

			str.AppendLine();
			WriteLocE(4, "Always 0?");
			str.AppendLine($" {r.ReadUInt32()},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" {r.ReadUInt32()},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" {r.ReadUInt32()},");

			int numPoints = ((data.Length - 181) / Point.LEN) + 2;

			for (int p = 0; p < numPoints; p++)
			{
				str.AppendLine();
				str.AppendLine(" // POINT " + (p + 1) + '/' + numPoints + ':');
				WriteLocE(4, "Amplitude");
				str.AppendLine($" {r.ReadSingle()}f,");
				WriteLocE(4, "Tension from previous point (-1 to 1)");
				str.AppendLine($" {r.ReadSingle()}f,");
				WriteLocE(4, "Curve type: 0x00000000 for first point? 0x01000000 for single curve edited? 0x02000000 for single curve? 0x00000002 for hold.");
				str.AppendLine($" 0x{r.ReadUInt32():X8},");
				WriteLocE(4, "IsLastPoint (0 or 1)");
				str.AppendLine($" {r.ReadUInt32()},");
				WriteLoc(4);
				str.AppendLine($" {r.ReadSingle()}f,");
				WriteLocE(4, "Next point related");
				str.AppendLine($" {r.ReadSingle()}f,");
			}

			str.AppendLine();
			str.AppendLine(" // Same for all:");
			WriteLocE(4, "Always -1?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always -1?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0x80 (128)?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0x80 (128)?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLoc(4);
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0x80 (128)?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 5?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 3?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 1?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 1.875?");
			str.AppendLine($" {r.ReadSingle()}f,");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 1?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always -1?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always -1?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always -1?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0xB2FB?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8},");
			WriteLocE(4, "Always 0?");
			str.AppendLine($" 0x{r.ReadUInt32():X8}");

			if (ms.Position != data.Length)
			{
				throw new Exception();
			}

			str.Append('}');
			return str.ToString();
		}
	}
}
