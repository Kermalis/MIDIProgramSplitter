using Kermalis.EndianBinaryIO;

namespace FLP;

internal struct FLChannelParams
{
	public static void WriteMIDIOut(EndianBinaryWriter w, ushort chanIndex)
	{
		Write(w, 1, (byte)chanIndex);
	}
	public static void WriteAutomation(EndianBinaryWriter w)
	{
		Write(w, 0, 1);
	}

	private static void Write(EndianBinaryWriter w, byte typeProbably, byte someID)
	{
		w.WriteEnum(FLEvent.ChannelParams);
		FLProjectWriter.WriteArrayEventLength(w, 168);

		w.WriteInt32(-1);
		w.WriteInt32(0);

		w.WriteUInt16(1);
		w.WriteByte(0);
		w.WriteByte(typeProbably);

		w.WriteInt32(-1);
		w.WriteUInt32(60); // Root Key: C5 => 60
		w.WriteSingle(1f);
		w.WriteSingle(1f);
		w.WriteSingle(1f);
		w.WriteSingle(1f);
		w.WriteSingle(1f);
		w.WriteInt32((int)ArpDirection.Off); // Arp Direction
		w.WriteInt32(1); // Arp Range
		w.WriteInt32(-1); // Arp Chord: Major => 1 | Autosustain => -1
		w.WriteUInt32(0x400); // Arp Time: 0:03 => 0x1C2 (450) | 1:00 => 0x400 (1024) | 4:00 => 0x5A6 (1446) | Hold => 0x5A7 (1447)
		w.WriteUInt32(48); // Arp Gate: 0% => 0, 100% => 48

		w.WriteByte(0); // Arp Slide
		w.WriteByte(0);
		w.WriteByte(0); // Was 0 for a default sampler
		w.WriteByte(0);

		w.WriteUInt32(0x5A7); // ? 1447 like Arp Time above
		w.WriteInt32(0);
		w.WriteUInt32(0x100);
		w.WriteInt32(0);
		w.WriteInt32(0);
		w.WriteInt32(0);
		w.WriteInt32(0);
		w.WriteInt32(1); // Arp Repeat
		w.WriteInt32(0);
		w.WriteInt32(0);
		w.WriteInt32(0);
		w.WriteInt32(0);
		w.WriteInt32(2);
		w.WriteInt32(-2);
		w.WriteInt32(-1);
		w.WriteInt32(0);
		w.WriteDouble(0d);
		w.WriteDouble(1d);
		w.WriteDouble(0d);
		w.WriteInt32(-1);

		w.WriteByte(someID); // TODO: What if the channel ID is larger than 255?
		w.WriteByte(1);
		w.WriteUInt16(0);

		w.WriteDouble(0.5d);
	}
}
