using System;

namespace MIDIProgramSplitter.FLP;

internal enum FLChanType : byte
{
	Sampler,
	TS404,
	Osc3x_MIDIOut,
	Layer,
	Automation = 5,
}

[Flags]
internal enum FLFadeStereo : ushort
{
	None = 0,
	SampleReversed = 1 << 1,
	SampleReverseStereo = 1 << 8,
}

internal enum FLMixerParamsEvent : byte
{
	SlotState = 0x00,
	SlotVolume = 0x01,
	SlotDryWet = 0x02,
	Unk_A4 = 0xA4,
	Unk_A5 = 0xA5,
	Unk_A6 = 0xA6,
	Unk_A7 = 0xA7,
	Unk_A8 = 0xA8,
	Unk_BE = 0xBE,
	Volume = 0xC0,
	Pan = 0xC1,
	StereoSeparation = 0xC2,
	LowLevel = 0xD0,
	BandLevel = 0xD1,
	HighLevel = 0xD2,
	LowFreq = 0xD8,
	BandFreq = 0xD9,
	HighFreq = 0xDA,
	LowWidth = 0xE0,
	BandWidth = 0xE1,
	HighWidth = 0xE2,
}

[Flags]
internal enum InsertFlags : ushort
{
	None = 0,
	ReversePolarity = 1 << 0,
	SwapChannels = 1 << 1,
	Unknown3 = 1 << 2,
	Unmute = 1 << 3,
	DisableThreaded = 1 << 4,
	Unknown6 = 1 << 5,
	DockedMiddle = 1 << 6,
	DockedRight = 1 << 7,
	Unknown9 = 1 << 8,
	Unknown10 = 1 << 9,
	Separator = 1 << 10,
	Lock = 1 << 11,
	Solo = 1 << 12,
	Unknown14 = 1 << 13,
	Unknown15 = 1 << 14,
	Unknown16 = 1 << 15,
}