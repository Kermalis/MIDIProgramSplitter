using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FLP;

public sealed partial class FLAutomation
{
	public enum MyType : byte
	{
		Volume,
		Panpot,
		Pitch,
		MIDIProgram,
		Tempo,
	}

	private const int TWO_POINT_LEN = 181;
	private const int NO_POINT_LEN = TWO_POINT_LEN - (2 * Point.LEN);

	private static ReadOnlySpan<byte> ChanPoly => new byte[9]
	{
		0x01, 0x00, 0x00, 0x00,
		0xF4, 0x01, // 500. TODO: Is this related to the number of playlist tracks?
		0x00, 0x00,
		0x00
	};

	internal ushort Index;

	public string Name;
	public FLColor3 Color;
	public readonly MyType Type;
	/// <summary>Only null for Tempo</summary>
	public readonly List<FLChannel>? Targets;
	public readonly List<Point> Points;
	public FLChannelFilter Filter;

	internal FLAutomation(string name, MyType type, List<FLChannel>? targets, FLChannelFilter filter)
	{
		Name = name;
		Color = new FLColor3(0x60608E); // R 142, G 96, B 96
		Type = type;
		Targets = targets;
		Points = new List<Point>();
		Filter = filter;
	}

	public void AddPoint(uint ticks, double value)
	{
		Points.Add(new Point
		{
			AbsoluteTicks = ticks,
			Value = value,
		});
	}
	public void AddTempoPoint(uint ticks, decimal bpm)
	{
		// Default min/max (10%/33%): 60 is 0.0d, 120 is 0.5d, 140 is 0.6666666865348816d, 180 is 1.0d
		// Min/Max 0%/100%: 0.0d is 10, 0.5d is 266, 1.0d is 522

		AddPoint(ticks, (double)FLUtils.LerpUnclamped(10, 522, 0, 1, bpm));
	}
	public void PadPoints(uint targetTicks, double defaultValue)
	{
		if (Points.Count == 0)
		{
			throw new Exception();
		}

		// Make sure there's a point at 0
		Point firstPoint = Points[0];
		if (firstPoint.AbsoluteTicks != 0)
		{
			Points.Insert(0, new Point
			{
				AbsoluteTicks = 0,
				Value = defaultValue,
			});
		}

		// Make sure there's a point at targetTicks
		Point lastPoint = Points[Points.Count - 1];
		if (lastPoint.AbsoluteTicks != targetTicks)
		{
			AddPoint(targetTicks, lastPoint.Value);
		}
	}
	public void PadTempoPoints(uint targetTicks, double defaultTempo)
	{
		PadPoints(targetTicks, FLUtils.LerpUnclamped(10, 522, 0, 1, defaultTempo));
	}

	internal void Write(EndianBinaryWriter w, uint ppqn)
	{
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewChannel, Index);
		FLProjectWriter.Write8BitEvent(w, FLEvent.ChannelType, (byte)FLChannelType.Automation);
		FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.DefPluginName, "\0");
		FLNewPlugin.WriteAutomation(w);
		FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.PluginName, Name + '\0');
		FLProjectWriter.Write32BitEvent(w, FLEvent.PluginIcon, 0);
		FLProjectWriter.Write32BitEvent(w, FLEvent.PluginColor, Color.GetFLValue());
		// No plugin params
		FLProjectWriter.Write8BitEvent(w, FLEvent.ChannelIsEnabled, 1);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.Delay, FLChannel.Delay);
		FLProjectWriter.Write32BitEvent(w, FLEvent.DelayReso, 0x800_080);
		FLProjectWriter.Write32BitEvent(w, FLEvent.Reverb, 0x10_000);
		FLProjectWriter.Write16BitEvent(w, FLEvent.ShiftDelay, 0);
		FLProjectWriter.Write16BitEvent(w, FLEvent.SwingMix, 0x80);
		FLProjectWriter.Write16BitEvent(w, FLEvent.FX, 0x80);
		FLProjectWriter.Write16BitEvent(w, FLEvent.FX3, 0x100);
		FLProjectWriter.Write16BitEvent(w, FLEvent.CutOff, 0x400);
		FLProjectWriter.Write16BitEvent(w, FLEvent.Resonance, 0);
		FLProjectWriter.Write16BitEvent(w, FLEvent.PreAmp, 0);
		FLProjectWriter.Write16BitEvent(w, FLEvent.Decay, 0);
		FLProjectWriter.Write16BitEvent(w, FLEvent.Attack, 0);
		FLProjectWriter.Write16BitEvent(w, FLEvent.StDel, 0x800);
		FLProjectWriter.Write32BitEvent(w, FLEvent.FXSine, 0x800_000);
		FLProjectWriter.Write16BitEvent(w, FLEvent.Fade_Stereo, (ushort)FLFadeStereo.None);
		FLProjectWriter.Write8BitEvent(w, FLEvent.TargetFXTrack, 0);
		FLBasicChannelParams.WriteAutomation(w);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChanOfsLevels, FLChannel.ChanOfsLevels);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChanPoly, ChanPoly);
		FLChannelParams.WriteAutomation(w);
		FLProjectWriter.Write32BitEvent(w, FLEvent.CutCutBy, 0);
		FLProjectWriter.Write32BitEvent(w, FLEvent.ChannelLayerFlags, 0);
		FLProjectWriter.Write32BitEvent(w, FLEvent.ChanFilterNum, Filter.Index);
		WriteAutomationData(w, ppqn);
		FLProjectWriter.Write8BitEvent(w, FLEvent.Unk_32, 0);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChannelTracking, FLChannel.Tracking0);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChannelTracking, FLChannel.Tracking1);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.Envelope1);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProjectWriter.WriteArrayEventWithLength(w, FLEvent.ChannelEnvelope, FLChannel.EnvelopeOther);
		FLProjectWriter.Write32BitEvent(w, FLEvent.ChannelSampleFlags, 0b0011);
		FLProjectWriter.Write8BitEvent(w, FLEvent.ChannelLoopType, 0);
	}
	private void WriteAutomationData(EndianBinaryWriter w, uint ppqn)
	{
		w.WriteEnum(FLEvent.AutomationData);

		uint numPoints = (uint)Points.Count;
		FLProjectWriter.WriteArrayEventLength(w, NO_POINT_LEN + (numPoints * Point.LEN));

		w.WriteUInt32(1);
		w.WriteUInt32(0x40);
		w.WriteByte(MyTypeToAutoType(Type));
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
	internal void WriteAutomationConnection(EndianBinaryWriter w)
	{
		if (Type == MyType.Tempo)
		{
			WriteAutomationConnection(w, 0x4000);
		}
		else
		{
			foreach (FLChannel target in Targets!)
			{
				WriteAutomationConnection(w, target.Index);
			}
		}
	}
	private void WriteAutomationConnection(EndianBinaryWriter w, ushort target)
	{
		w.WriteEnum(FLEvent.AutomationConnection);
		FLProjectWriter.WriteArrayEventLength(w, 20);

		w.WriteUInt16(0);
		w.WriteUInt16(Index);
		w.WriteUInt32(0);
		w.WriteUInt16(MyTypeToConnectionType(Type));
		w.WriteUInt16(target);
		w.WriteUInt32(8);
		w.WriteUInt32(0x1D5);
	}
	private static byte MyTypeToAutoType(MyType t)
	{
		switch (t)
		{
			case MyType.Volume:
			case MyType.MIDIProgram:
			case MyType.Tempo:
				return 0;
			case MyType.Panpot:
			case MyType.Pitch:
				return 1;
		}
		throw new ArgumentOutOfRangeException(nameof(t), t, null);
	}
	private static ushort MyTypeToConnectionType(MyType t)
	{
		switch (t)
		{
			case MyType.Volume: return 0x0000;
			case MyType.Panpot: return 0x0001;
			case MyType.Pitch: return 0x0004;
			case MyType.Tempo: return 0x0005;
			case MyType.MIDIProgram: return 0x8000;
		}
		throw new ArgumentOutOfRangeException(nameof(t), t, null);
	}

	internal static string ReadData(byte[] data)
	{
		using (MemoryStream ms = new(data))
		{
			var r = new EndianBinaryReader(ms);
			StringBuilder str = new();

			void WriteLoc(int numBytes, string extra)
			{
				long i = ms.Position;
				str.AppendLine(string.Format(" // {0:D3}-{1:D3}: {2}", i, i + numBytes - 1, extra));
			}
			void WriteLoc1(string extra)
			{
				long i = ms.Position;
				str.AppendLine(string.Format(" // {0:D3}: {1}", i, extra));
			}

			str.AppendLine("{");

			WriteLoc(4, "Always 1?");
			uint v0 = r.ReadUInt32();
			str.AppendLine($" {v0},");
			if (v0 != 1)
			{
				;
			}

			WriteLoc(4, "Always 0x40 (64)?");
			uint v4 = r.ReadUInt32();
			str.AppendLine($" 0x{v4:X},");
			if (v4 != 0x40)
			{
				;
			}

			str.AppendLine();
			WriteLoc1("0 for vol/gminstrument/tempo, 1 for pan/pitch");
			byte type = r.ReadByte();
			str.AppendLine($" 0x{type:X2},");
			if (type is not 0 and not 1)
			{
				;
			}

			str.AppendLine();
			WriteLoc(4, "Always 4?");
			uint v9 = r.ReadUInt32();
			str.AppendLine($" {v9},");
			if (v9 != 4)
			{
				;
			}
			WriteLoc(4, "Always 3?");
			uint v13 = r.ReadUInt32();
			str.AppendLine($" {v13},");
			if (v13 != 3)
			{
				;
			}

			str.AppendLine();
			WriteLoc(4, "Num points");
			uint nPoints = r.ReadUInt32();
			str.AppendLine($" {nPoints},");

			str.AppendLine();
			WriteLoc(4, "Always 0?");
			uint v21 = r.ReadUInt32();
			str.AppendLine($" {v21},");
			if (v21 != 0)
			{
				;
			}
			WriteLoc(4, "Always 0?");
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

				WriteLoc(8, "Amplitude");
				str.AppendLine($" {r.ReadDouble()}d,");

				// Tension and curve type are in buggy states when you convert events to an automation clip. Not recreating that behavior
				WriteLoc(4, "Tension from previous point (-1 to 1)");
				float tension = r.ReadSingle();
				uint fAsU = BitConverter.SingleToUInt32Bits(tension);
				str.AppendLine($" {tension}f, // 0x{fAsU:X8}");

				WriteLoc(4, "Curve type: 0x00000000 for first point? 0x01000000 for single curve edited? 0x02000000 for single curve? 0x00000002 for hold.");
				str.AppendLine($" 0x{r.ReadUInt32():X8},");

				// For the last point, its hex value is 0xFFFFFFFF00000001, which is not a normal NaN.
				// Normal NaN is 0x7FF8000000000000
				WriteLoc(8, "Next point deltatime (in quarters of a bar)");
				double nextPointDelta = r.ReadDouble();
				ulong dAsUL = BitConverter.DoubleToUInt64Bits(nextPointDelta);
				str.AppendLine($" {nextPointDelta}d, // 0x{dAsUL:X16}");
			}

			str.AppendLine();
			str.AppendLine(" // Same for all:");
			WriteLoc(4, "Always -1?");
			uint v77 = r.ReadUInt32();
			str.AppendLine($" 0x{v77:X8},");
			if (v77 != uint.MaxValue)
			{
				;
			}
			WriteLoc(4, "Always -1?");
			uint v81 = r.ReadUInt32();
			str.AppendLine($" 0x{v81:X8},");
			if (v81 != uint.MaxValue)
			{
				;
			}
			WriteLoc(4, "Always -1?");
			uint v85 = r.ReadUInt32();
			str.AppendLine($" 0x{v85:X8},");
			if (v85 != uint.MaxValue)
			{
				;
			}
			WriteLoc(4, "Always 0x80 (128)?");
			uint v89 = r.ReadUInt32();
			str.AppendLine($" 0x{v89:X8},");
			if (v89 != 0x80)
			{
				;
			}
			WriteLoc(4, "Always 0x80 (128)?");
			uint v93 = r.ReadUInt32();
			str.AppendLine($" 0x{v93:X8},");
			if (v93 != 0x80)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v97 = r.ReadUInt32();
			str.AppendLine($" 0x{v97:X8},");
			if (v97 != 0)
			{
				;
			}
			WriteLoc(4, "Always 0x80 (128)?");
			uint v101 = r.ReadUInt32();
			str.AppendLine($" 0x{v101:X8},");
			if (v101 != 0x80)
			{
				;
			}
			WriteLoc(4, "Always 5?");
			uint v105 = r.ReadUInt32();
			str.AppendLine($" 0x{v105:X8},");
			if (v105 != 5)
			{
				;
			}
			WriteLoc(4, "Always 3?");
			uint v109 = r.ReadUInt32();
			str.AppendLine($" 0x{v109:X8},");
			if (v109 != 3)
			{
				;
			}
			WriteLoc(4, "Always 1?");
			uint v113 = r.ReadUInt32();
			str.AppendLine($" 0x{v113:X8},");
			if (v113 != 1)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v117 = r.ReadUInt32();
			str.AppendLine($" 0x{v117:X8},");
			if (v117 != 0)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v121 = r.ReadUInt32();
			str.AppendLine($" 0x{v121:X8},");
			if (v121 != 0)
			{
				;
			}
			WriteLoc(8, "Always 1?");
			double v125 = r.ReadDouble();
			str.AppendLine($" {v125}d,");
			if (v125 != 1)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v133 = r.ReadUInt32();
			str.AppendLine($" 0x{v133:X8},");
			if (v133 != 0)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v137 = r.ReadUInt32();
			str.AppendLine($" 0x{v137:X8},");
			if (v137 != 0)
			{
				;
			}
			WriteLoc(4, "Always 1?");
			uint v141 = r.ReadUInt32();
			str.AppendLine($" 0x{v141:X8},");
			if (v141 != 1)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v145 = r.ReadUInt32();
			str.AppendLine($" 0x{v145:X8},");
			if (v145 != 0)
			{
				;
			}
			WriteLoc(4, "Always -1?");
			uint v149 = r.ReadUInt32();
			str.AppendLine($" 0x{v149:X8},");
			if (v149 != uint.MaxValue)
			{
				;
			}
			WriteLoc(4, "Always -1?");
			uint v151 = r.ReadUInt32();
			str.AppendLine($" 0x{v151:X8},");
			if (v151 != uint.MaxValue)
			{
				;
			}
			WriteLoc(4, "Always -1?");
			uint v155 = r.ReadUInt32();
			str.AppendLine($" 0x{v155:X8},");
			if (v155 != uint.MaxValue)
			{
				;
			}
			WriteLoc(4, "Always 0xB2FB (45_819)?");
			uint v157 = r.ReadUInt32();
			str.AppendLine($" 0x{v157:X8},");
			if (v157 != 0xB2FB)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v161 = r.ReadUInt32();
			str.AppendLine($" 0x{v161:X8},");
			if (v161 != 0)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v165 = r.ReadUInt32();
			str.AppendLine($" 0x{v165:X8},");
			if (v165 != 0)
			{
				;
			}
			WriteLoc(4, "Always 0?");
			uint v169 = r.ReadUInt32();
			str.AppendLine($" 0x{v169:X8},");
			if (v169 != 0)
			{
				;
			}
			WriteLoc(4, "Always 0?");
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
