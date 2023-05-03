﻿using Kermalis.EndianBinaryIO;
using Kermalis.MIDI;
using System;

namespace MIDIProgramSplitter.FLP;

public sealed class FLChannel
{
	private static ReadOnlySpan<byte> NewPlugin_DeselectedTopLeft => new byte[52]
	{
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x54, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x04, 0x00, // Selection/pos related. Was 0x0112 when selected, then 0x0004 when deselected in the same pos.
		0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00
	};

	internal static ReadOnlySpan<byte> Delay => new byte[20] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x90, 0x00, 0x00, 0x00 };
	private static ReadOnlySpan<byte> BasicChanParams => new byte[24] { 0x00, 0x19, 0x00, 0x00, 0x10, 0x27, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	internal static ReadOnlySpan<byte> ChanOfsLevels => new byte[20] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	private static ReadOnlySpan<byte> ChanPoly => new byte[9] { 0x00, 0x00, 0x00, 0x00, 0xF4, 0x01, 0x00, 0x00, 0x00 };
	internal static ReadOnlySpan<byte> Tracking0 => new byte[16] { 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	internal static ReadOnlySpan<byte> Tracking1 => new byte[16] { 0x3C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

	internal static ReadOnlySpan<byte> EnvelopeOther => new byte[68]
	{
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00,
		0x20, 0x4E, 0x00, 0x00, 0x30, 0x75, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0xB6, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00
	};
	internal static ReadOnlySpan<byte> Envelope1 => new byte[68]
	{
		0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00,
		0x20, 0x4E, 0x00, 0x00, 0x30, 0x75, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x20, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0xB6, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x9B, 0xFF, 0xFF, 0xFF
	};

	private static ReadOnlySpan<byte> PluginParamsPart1 => new byte[4] { 0x06, 0x00, 0x00, 0x00 };
	private static ReadOnlySpan<byte> PluginParamsPart2 => new byte[25]
	{
		0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00
	};
	private static ReadOnlySpan<byte> PluginParamsPart3 => new byte[352]
	{
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0xFF, 0x06, 0x50, 0x61, 0x67, 0x65, 0x20, 0x31, 0x06, 0x50, 0x61, 0x67, 0x65,
		0x20, 0x32, 0x06, 0x50, 0x61, 0x67, 0x65, 0x20, 0x33, 0x06, 0x50, 0x61, 0x67, 0x65, 0x20, 0x34,
		0x06, 0x50, 0x61, 0x67, 0x65, 0x20, 0x35, 0x06, 0x50, 0x61, 0x67, 0x65, 0x20, 0x36, 0x06, 0x50,
		0x61, 0x67, 0x65, 0x20, 0x37, 0x06, 0x50, 0x61, 0x67, 0x65, 0x20, 0x38, 0x00, 0x00, 0x00, 0x00
	};

	private static ReadOnlySpan<byte> ChanParamsPart1 => new byte[156]
	{
		0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0xFF, 0xFF, 0xFF, 0xFF,
		0x3C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F,
		0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
		0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x04, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00,
		0xA7, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x02, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF,
	};
	private static ReadOnlySpan<byte> ChanParamsPart2 => new byte[11] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x3F };

	public readonly string Name;
	public readonly byte MIDIChannel;
	public readonly MIDIProgram MIDIProgram;

	public FLChannel(string name, byte midiChan, MIDIProgram midiProgram)
	{
		Name = name;
		MIDIChannel = midiChan;
		MIDIProgram = midiProgram;
	}

	internal void Write(EndianBinaryWriter w, ushort id, uint filterNum)
	{
		FLProjectWriter.WriteWordEvent(w, FLEvent.NewChannel, id);
		FLProjectWriter.WriteByteEvent(w, FLEvent.ChannelType, (byte)FLChanType.Osc3x_MIDIOut);
		FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.DefPluginName, "MIDI Out\0");
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.NewPlugin, NewPlugin_DeselectedTopLeft);
		FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.PluginName, Name + '\0');
		FLProjectWriter.WriteDWordEvent(w, FLEvent.PluginIcon, 0);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Color, 0x73725E);
		WritePluginParams(w);
		FLProjectWriter.WriteByteEvent(w, FLEvent.ChannelIsEnabled, 1);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.Delay, Delay);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.DelayReso, 0x800_080);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Reverb, 0x10_000);
		FLProjectWriter.WriteWordEvent(w, FLEvent.ShiftDelay, 0);
		FLProjectWriter.WriteWordEvent(w, FLEvent.SwingMix, 0x80);
		FLProjectWriter.WriteWordEvent(w, FLEvent.FX, 0x80);
		FLProjectWriter.WriteWordEvent(w, FLEvent.FX3, 0x100);
		FLProjectWriter.WriteWordEvent(w, FLEvent.CutOff, 0x400);
		FLProjectWriter.WriteWordEvent(w, FLEvent.Resonance, 0);
		FLProjectWriter.WriteWordEvent(w, FLEvent.PreAmp, 0);
		FLProjectWriter.WriteWordEvent(w, FLEvent.Decay, 0);
		FLProjectWriter.WriteWordEvent(w, FLEvent.Attack, 0);
		FLProjectWriter.WriteWordEvent(w, FLEvent.StDel, 0x800);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.FXSine, 0x800_000);
		FLProjectWriter.WriteWordEvent(w, FLEvent.Fade_Stereo, 0);
		FLProjectWriter.WriteByteEvent(w, FLEvent.TargetFXTrack, 0);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.BasicChanParams, BasicChanParams);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChanOfsLevels, ChanOfsLevels);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChanPoly, ChanPoly);
		WriteChanParams(w, id);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.CutCutBy, (uint)(id + 1) * 0x10_001u);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.ChannelLayerFlags, 0);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.ChanFilterNum, filterNum);
		FLProjectWriter.WriteByteEvent(w, FLEvent.Unk_32, 0);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChannelTracking, Tracking0);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChannelTracking, Tracking1);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, EnvelopeOther);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, Envelope1);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, EnvelopeOther);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, EnvelopeOther);
		FLProjectWriter.WriteBytesEventWithLength(w, FLEvent.ChannelEnvelope, EnvelopeOther);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.ChannelSampleFlags, 0b1010);
		FLProjectWriter.WriteByteEvent(w, FLEvent.ChannelLoopType, 0);
	}
	private void WritePluginParams(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.PluginParams);
		FLProjectWriter.WriteTextEventLength(w, 383);
		w.WriteBytes(PluginParamsPart1);
		w.WriteByte(MIDIChannel);
		w.WriteBytes(PluginParamsPart2);
		w.WriteByte((byte)(MIDIProgram + 1));
		w.WriteBytes(PluginParamsPart3);
	}
	private static void WriteChanParams(EndianBinaryWriter w, ushort id)
	{
		w.WriteEnum(FLEvent.ChannelParams);
		FLProjectWriter.WriteTextEventLength(w, 168);
		w.WriteBytes(ChanParamsPart1);
		w.WriteByte((byte)id);
		w.WriteBytes(ChanParamsPart2);
	}
}
