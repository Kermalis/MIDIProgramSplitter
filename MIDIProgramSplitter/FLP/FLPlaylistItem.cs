﻿using Kermalis.EndianBinaryIO;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLPlaylistItem
{
	public const int LEN = 32;

	// For patterns:
	// @00-03 AbsoluteTick
	// @04-05 ? Always 0x5000
	// @06-07: 0x50xx = Pattern, 0x00xx = AutomationChannel
	// @08-11 DurationTicks
	// @12-13 500-val = PlaylistTrack from 1index
	// @14-15 ? Always 0
	// @16-17 ? Always 120 (0x0078)
	// @18 ? Always 0x40
	// @19 IsSelected. 0x80 if selected, 0x00 if deselected, 0x20 if disabled and deselected, 0xA0 if disabled and selected
	// @20 ? Always 0x40
	// @21 ? Always 0x64
	// @22-23 ? Always 0x8080
	// @24-31 ? All 0xFF for patterns, 0xBF800000 0xBF800000 for automations

	// Pat1 at track0 1:01:00
	// [0x00, 0x00, 0x00, 0x00, 0x00, 0x50, 0x01, 0x50, // 00-07
	//  0x80, 0x01, 0x00, 0x00, 0xF3, 0x01, 0x00, 0x00, // 08-15
	//  0x78, 0x00, 0x40, 0x00, 0x40, 0x64, 0x80, 0x80, // 16-23
	//  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF] // 24-31

	// Pat2 at track1 1:05:00
	//  0x60, 0x00, 0x00, 0x00, 0x00, 0x50, 0x02, 0x50,
	//  0x80, 0x01, 0x00, 0x00, 0xF2, 0x01, 0x00, 0x00,
	//  0x78, 0x00, 0x40, 0x80, 0x40, 0x64, 0x80, 0x80,
	//  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]

	// [0x00, 0x00, 0x00, 0x00, 0x00, 0x50, 0x03, 0x00,
	//  0xE0, 0x01, 0x00, 0x00, 0xF1, 0x01, 0x00, 0x00,
	//  0x78, 0x00, 0x40, 0x00, 0x40, 0x64, 0x80, 0x80,
	//  0x00, 0x00, 0x80, 0xBF, 0x00, 0x00, 0x80, 0xBF,

	//  0x00, 0x00, 0x00, 0x00, 0x00, 0x50, 0x01, 0x50,
	//  0x80, 0x01, 0x00, 0x00, 0xF3, 0x01, 0x00, 0x00,
	//  0x78, 0x00, 0x40, 0x00, 0x40, 0x64, 0x80, 0x80,
	//  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,

	//  0x60, 0x00, 0x00, 0x00, 0x00, 0x50, 0x02, 0x50,
	//  0x80, 0x01, 0x00, 0x00, 0xF2, 0x01, 0x00, 0x00,
	//  0x78, 0x00, 0x40, 0x00, 0x40, 0x64, 0x80, 0x80,
	//  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]

	public uint AbsoluteTick;
	public byte Pattern1Indexed;
	public uint DurationTicks;
	public ushort PlaylistTrack1Indexed;

	public void Write(EndianBinaryWriter w)
	{
		w.WriteUInt32(AbsoluteTick);
		w.WriteUInt16(0x5000);
		w.WriteByte(Pattern1Indexed);
		w.WriteByte(0x50);
		w.WriteUInt32(DurationTicks);
		w.WriteUInt16((ushort)(500 - PlaylistTrack1Indexed));
		w.WriteUInt16(0);
		w.WriteUInt16(120);
		w.WriteByte(0x40);
		w.WriteByte(0);
		w.WriteByte(0x40);
		w.WriteByte(0x64);
		w.WriteUInt16(0x8080);
		w.WriteUInt32(uint.MaxValue);
		w.WriteUInt32(uint.MaxValue);
	}
}