using Kermalis.EndianBinaryIO;

namespace MIDIProgramSplitter.FLP;

internal struct FLPatternNote
{
	public const int LEN = 24;

	// This is just D5 at 0 pos:
	// @00/01/02/03: Position
	// @04: Slide
	// @05 ? Always 0x40
	// @06/07: Channel
	// @08/09/10/11: Duration. 0x0 => infinite, 0x000018 => 0bar1step0tick (24ticks), 0x000030 => 0bar2step0tick (48ticks), 0x000078 => 0bar5step0tick (120ticks), 0x000168 => 0bar15step0tick (360ticks), 0x3A9800 10000bar0step0tick (3,840,000ticks)
	// @12: Key
	// @13/14/15 ? Always 0
	// @16: Pitch
	// @17 ? Always 0
	// @18: Release
	// @19: Color/Portamento
	// @20: Pan
	// @21: Velocity
	// @22: ModX
	// @23: ModY
	// @ 0xBA		Bytes: PatternNotes - 24 =
	// [0x00, 0x00, 0x00, 0x00, // 00-03
	//  0x00, 0x40, 0x00, 0x00, // 04-07
	//  0x30, 0x00, 0x00, 0x00, // 08-11
	//  0x3E, 0x00, 0x00, 0x00, // 12-15
	//  0x78, 0x00, 0x40, 0x00, // 16-19
	//  0x40, 0x64, 0x80, 0x80] // 20-23

	public uint AbsoluteTick;
	/// <summary>Disabled => 0, Enabled => 8</summary>
	public byte Slide;
	/// <summary>Always 0x40</summary>
	public byte Unk5 = 0x40;
	public ushort Channel;
	/// <summary>Infinite => 0</summary>
	public uint DurationTicks = 48;
	/// <summary>C5 => 60</summary>
	public uint Key;
	/// <summary>-1200 => 000, 0 => 120, +1200 => 240</summary>
	public byte Pitch = 120;
	public byte Unk17;
	/// <summary>0% => 0x00, 50% => 0x40, 100% => 0x80</summary>
	public byte Release = 0x40;
	/// <summary>0 through F are colors with no porta, 0x10 is color0 with porta, 0x1F is colorF with porta</summary>
	public byte ColorPortamento;
	/// <summary>100% left => 0x00, center => 0x40, 100% right => 0x80</summary>
	public byte Panpot = 0x40;
	/// <summary>0% => 0x00, "80%" => 0x64, 100% => 0x80</summary>
	public byte Velocity = 0x64;
	/// <summary>-100 => 0x00, 0 => 0x80, +100 => 0xFF</summary>
	public byte ModX = 0x80;
	/// <summary>-100 => 0x00, 0 => 0x80, +100 => 0xFF</summary>
	public byte ModY = 0x80;

	public FLPatternNote()
	{
		//
	}

	public void Write(EndianBinaryWriter w)
	{
		w.WriteUInt32(AbsoluteTick);
		w.WriteByte(Slide);
		w.WriteByte(Unk5);
		w.WriteUInt16(Channel);
		w.WriteUInt32(DurationTicks);
		w.WriteUInt32(Key);
		w.WriteByte(Pitch);
		w.WriteByte(Unk17);
		w.WriteByte(Release);
		w.WriteByte(ColorPortamento);
		w.WriteByte(Panpot);
		w.WriteByte(Velocity);
		w.WriteByte(ModX);
		w.WriteByte(ModY);
	}
}
