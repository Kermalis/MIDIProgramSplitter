using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MIDIProgramSplitter.FLP;

internal sealed partial class FLAutomation
{
	public enum MyType : byte
	{
		Volume,
		Panpot,
		Pitch,
		MIDIProgram,
	}

	private const int TWO_POINT_LEN = 181;
	private const int NO_POINT_LEN = TWO_POINT_LEN - (2 * Point.LEN);

	private static ReadOnlySpan<byte> NewPlugin_DeselectedTopLeft => new byte[52]
	{
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
		0x54, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x06, 0x00,
		0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00
	};

	// Contains min/max values. Max length also?
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
	public readonly MyType Type;
	public readonly FLChannel Target;
	public readonly List<Point> Points;

	public FLAutomation(string name, MyType type, FLChannel target)
	{
		Name = name;
		Type = type;
		Target = target;
		Points = new List<Point>();
	}

	public void Write(EndianBinaryWriter w, ushort id, uint filterNum, uint ppqn)
	{
		FLProject.WriteWordEvent(w, FLEvent.NewChannel, id);
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
		WriteData(w, ppqn);
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
	private void WriteData(EndianBinaryWriter w, uint ppqn)
	{
		w.WriteEnum(FLEvent.AutomationData);

		uint numPoints = (uint)Points.Count;
		FLProject.WriteTextEventLength(w, NO_POINT_LEN + (numPoints * Point.LEN));

		byte type = 0; // TODO: Type

		w.WriteUInt32(1);
		w.WriteUInt32(0x40);
		w.WriteByte(type);
		w.WriteUInt32(4);
		w.WriteUInt32(3);
		w.WriteUInt32(numPoints);
		w.WriteUInt32(0);
		w.WriteUInt32(0);

		for (int i = 0; i < numPoints; i++)
		{
			bool isLast = i == numPoints - 1;
			Points[i].Write(w, ppqn, i == 0, isLast, isLast ? 0 : Points[i + 1].AbsoluteTicks);
		}

		w.WriteUInt32(uint.MaxValue);
		w.WriteUInt32(uint.MaxValue);
		w.WriteUInt32(uint.MaxValue);
		w.WriteUInt32(0x80);
		w.WriteUInt32(0x80);
		w.WriteUInt32(0);
		w.WriteUInt32(0x80);
		w.WriteUInt32(5);
		w.WriteUInt32(3);
		w.WriteUInt32(1);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteDouble(1d);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(1);
		w.WriteUInt32(0);
		w.WriteUInt32(uint.MaxValue);
		w.WriteUInt32(uint.MaxValue);
		w.WriteUInt32(uint.MaxValue);
		w.WriteUInt32(0xB2FB);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
	}
	public void WriteAutomationConnection(EndianBinaryWriter w, ushort automationChannelID, ReadOnlySpan<FLChannel> channels)
	{
		w.WriteEnum(FLEvent.AutomationConnection);
		FLProject.WriteTextEventLength(w, 20);

		w.WriteUInt16(0);
		w.WriteUInt16(automationChannelID);
		w.WriteUInt32(0);
		w.WriteUInt16(MyTypeToFLType(Type));
		w.WriteUInt16((ushort)GetTargetChannelID(channels));
		w.WriteUInt32(8);
		w.WriteUInt32(0x1D5);
	}
	private int GetTargetChannelID(ReadOnlySpan<FLChannel> channels)
	{
		for (int i = 0; i < channels.Length; i++)
		{
			if (channels[i] == Target)
			{
				return i;
			}
		}
		throw new Exception();
	}
	private static ushort MyTypeToFLType(MyType t)
	{
		switch (t)
		{
			case MyType.Volume: return 0x0000;
			case MyType.Panpot: return 0x0001;
			case MyType.Pitch: return 0x0004;
			case MyType.MIDIProgram: return 0x8000;
		}
		throw new ArgumentOutOfRangeException(nameof(t), t, null);
	}

	public static string ReadData(byte[] data)
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
			uint v0 = r.ReadUInt32();
			str.AppendLine($" {v0},");
			if (v0 != 1)
			{
				;
			}

			WriteLocE(4, "Always 0x40 (64)?");
			uint v4 = r.ReadUInt32();
			str.AppendLine($" 0x{v4:X},");
			if (v4 != 0x40)
			{
				;
			}

			str.AppendLine();
			WriteLocE1("0 for vol/gminstrument, 1 for pan/pitch");
			byte type = r.ReadByte();
			str.AppendLine($" 0x{type:X2},");
			if (type is not 0 and not 1)
			{
				;
			}

			str.AppendLine();
			WriteLocE(4, "Always 4?");
			uint v9 = r.ReadUInt32();
			str.AppendLine($" {v9},");
			if (v9 != 4)
			{
				;
			}
			WriteLocE(4, "Always 3?");
			uint v13 = r.ReadUInt32();
			str.AppendLine($" {v13},");
			if (v13 != 3)
			{
				;
			}

			str.AppendLine();
			WriteLocE(4, "Num points");
			uint nPoints = r.ReadUInt32();
			str.AppendLine($" {nPoints},");

			str.AppendLine();
			WriteLocE(4, "Always 0?");
			uint v21 = r.ReadUInt32();
			str.AppendLine($" {v21},");
			if (v21 != 0)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v25 = r.ReadUInt32();
			str.AppendLine($" {v25},");
			if (v25 != 0)
			{
				;
			}

			int numPoints = (data.Length - NO_POINT_LEN) / Point.LEN;
			if (numPoints != nPoints)
			{
				throw new Exception();
			}

			for (int p = 0; p < numPoints; p++)
			{
				str.AppendLine();
				str.AppendLine(" // POINT " + (p + 1) + '/' + numPoints + ':');

				WriteLocE(8, "Amplitude");
				str.AppendLine($" {r.ReadDouble()}d,");

				// Tension and curve type are in buggy states when you convert events to an automation clip. Not recreating that behavior
				WriteLocE(4, "Tension from previous point (-1 to 1)");
				float tension = r.ReadSingle();
				uint fAsU = BitConverter.SingleToUInt32Bits(tension);
				str.AppendLine($" {tension}f, // 0x{fAsU:X8}");

				WriteLocE(4, "Curve type: 0x00000000 for first point? 0x01000000 for single curve edited? 0x02000000 for single curve? 0x00000002 for hold.");
				str.AppendLine($" 0x{r.ReadUInt32():X8},");

				// For the last point, its hex value is 0xFFFFFFFF00000001, which is not a normal NaN.
				// Normal NaN is 0x7FF8000000000000
				WriteLocE(8, "Next point deltatime (in quarters of a bar)");
				double nextPointDelta = r.ReadDouble();
				ulong dAsUL = BitConverter.DoubleToUInt64Bits(nextPointDelta);
				str.AppendLine($" {nextPointDelta}d, // 0x{dAsUL:X16}");
			}

			str.AppendLine();
			str.AppendLine(" // Same for all:");
			WriteLocE(4, "Always -1?");
			uint v77 = r.ReadUInt32();
			str.AppendLine($" 0x{v77:X8},");
			if (v77 != uint.MaxValue)
			{
				;
			}
			WriteLocE(4, "Always -1?");
			uint v81 = r.ReadUInt32();
			str.AppendLine($" 0x{v81:X8},");
			if (v81 != uint.MaxValue)
			{
				;
			}
			WriteLocE(4, "Always -1?");
			uint v85 = r.ReadUInt32();
			str.AppendLine($" 0x{v85:X8},");
			if (v85 != uint.MaxValue)
			{
				;
			}
			WriteLocE(4, "Always 0x80 (128)?");
			uint v89 = r.ReadUInt32();
			str.AppendLine($" 0x{v89:X8},");
			if (v89 != 0x80)
			{
				;
			}
			WriteLocE(4, "Always 0x80 (128)?");
			uint v93 = r.ReadUInt32();
			str.AppendLine($" 0x{v93:X8},");
			if (v93 != 0x80)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v97 = r.ReadUInt32();
			str.AppendLine($" 0x{v97:X8},");
			if (v97 != 0)
			{
				;
			}
			WriteLocE(4, "Always 0x80 (128)?");
			uint v101 = r.ReadUInt32();
			str.AppendLine($" 0x{v101:X8},");
			if (v101 != 0x80)
			{
				;
			}
			WriteLocE(4, "Always 5?");
			uint v105 = r.ReadUInt32();
			str.AppendLine($" 0x{v105:X8},");
			if (v105 != 5)
			{
				;
			}
			WriteLocE(4, "Always 3?");
			uint v109 = r.ReadUInt32();
			str.AppendLine($" 0x{v109:X8},");
			if (v109 != 3)
			{
				;
			}
			WriteLocE(4, "Always 1?");
			uint v113 = r.ReadUInt32();
			str.AppendLine($" 0x{v113:X8},");
			if (v113 != 1)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v117 = r.ReadUInt32();
			str.AppendLine($" 0x{v117:X8},");
			if (v117 != 0)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v121 = r.ReadUInt32();
			str.AppendLine($" 0x{v121:X8},");
			if (v121 != 0)
			{
				;
			}
			WriteLocE(8, "Always 1?");
			double v125 = r.ReadDouble();
			str.AppendLine($" {v125}d,");
			if (v125 != 1)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v133 = r.ReadUInt32();
			str.AppendLine($" 0x{v133:X8},");
			if (v133 != 0)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v137 = r.ReadUInt32();
			str.AppendLine($" 0x{v137:X8},");
			if (v137 != 0)
			{
				;
			}
			WriteLocE(4, "Always 1?");
			uint v141 = r.ReadUInt32();
			str.AppendLine($" 0x{v141:X8},");
			if (v141 != 1)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v145 = r.ReadUInt32();
			str.AppendLine($" 0x{v145:X8},");
			if (v145 != 0)
			{
				;
			}
			WriteLocE(4, "Always -1?");
			uint v149 = r.ReadUInt32();
			str.AppendLine($" 0x{v149:X8},");
			if (v149 != uint.MaxValue)
			{
				;
			}
			WriteLocE(4, "Always -1?");
			uint v151 = r.ReadUInt32();
			str.AppendLine($" 0x{v151:X8},");
			if (v151 != uint.MaxValue)
			{
				;
			}
			WriteLocE(4, "Always -1?");
			uint v155 = r.ReadUInt32();
			str.AppendLine($" 0x{v155:X8},");
			if (v155 != uint.MaxValue)
			{
				;
			}
			WriteLocE(4, "Always 0xB2FB (45_819)?");
			uint v157 = r.ReadUInt32();
			str.AppendLine($" 0x{v157:X8},");
			if (v157 != 0xB2FB)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v161 = r.ReadUInt32();
			str.AppendLine($" 0x{v161:X8},");
			if (v161 != 0)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v165 = r.ReadUInt32();
			str.AppendLine($" 0x{v165:X8},");
			if (v165 != 0)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v169 = r.ReadUInt32();
			str.AppendLine($" 0x{v169:X8},");
			if (v169 != 0)
			{
				;
			}
			WriteLocE(4, "Always 0?");
			uint v173 = r.ReadUInt32();
			str.AppendLine($" 0x{v173:X8}");
			if (v173 != 0)
			{
				;
			}

			if (ms.Position != data.Length)
			{
				throw new Exception();
			}

			str.Append('}');
			return str.ToString();
		}
	}
}
