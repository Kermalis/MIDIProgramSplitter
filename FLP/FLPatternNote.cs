using Kermalis.EndianBinaryIO;
using Kermalis.MIDI;

namespace FLP;

public struct FLPatternNote
{
	internal const int LEN = 24;

	public uint AbsoluteTick;
	public bool Slide;
	public FLChannel Channel;
	/// <summary>Infinite => 0</summary>
	public uint DurationTicks = 48;
	public MIDINote Key;
	/// <summary>-1200 => 000, 0 => 120, +1200 => 240</summary>
	public byte Pitch = 120;
	/// <summary>0% => 0x00, 50% => 0x40, 100% => 0x80</summary>
	public byte Release = 0x40;
	/// <summary>0 through F</summary>
	public byte Color;
	public bool Portamento;
	/// <summary>100% left => 0x00, center => 0x40, 100% right => 0x80</summary>
	public byte Panpot = 0x40;
	/// <summary>0% => 0x00, "80%" => 0x64, 100% => 0x80</summary>
	public byte Velocity = 0x64;
	/// <summary>-100 => 0x00, 0 => 0x80, +100 => 0xFF</summary>
	public byte ModX = 0x80;
	/// <summary>-100 => 0x00, 0 => 0x80, +100 => 0xFF</summary>
	public byte ModY = 0x80;

	public FLPatternNote(FLChannel chan)
	{
		Channel = chan;
	}

	public void Write(EndianBinaryWriter w)
	{
		w.WriteUInt32(AbsoluteTick);

		w.WriteByte((byte)(Slide ? 8 : 0));
		w.WriteByte(0x40);
		w.WriteUInt16(Channel.Index);

		w.WriteUInt32(DurationTicks);
		w.WriteUInt32((uint)Key);

		w.WriteByte(Pitch);
		w.WriteByte(0);
		w.WriteByte(Release);
		// 0 through F are colors with no porta, 0x10 is color0 with porta, 0x1F is colorF with porta
		w.WriteByte((byte)(Color + (Portamento ? 0x10 : 0)));

		w.WriteByte(Panpot);
		w.WriteByte(Velocity);
		w.WriteByte(ModX);
		w.WriteByte(ModY);
	}
}
