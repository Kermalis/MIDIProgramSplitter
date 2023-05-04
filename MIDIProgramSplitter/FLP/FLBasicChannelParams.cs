using Kermalis.EndianBinaryIO;

namespace MIDIProgramSplitter.FLP;

internal struct FLBasicChannelParams
{
	public static void WriteChannel(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.BasicChannelParams);
		FLProjectWriter.WriteArrayEventLength(w, 24);

		w.WriteUInt32(6_400); // Pan center (max 12_800)
		w.WriteUInt32(10_000); // 78.125% volume (max 12_800)

		w.WriteUInt32(0); // Always 0?
		w.WriteUInt32(0x100); // Always 0x100?
		w.WriteUInt32(0); // Always 0?
		w.WriteUInt32(0); // Always 0?
	}
	public static void WriteAutomation(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.BasicChannelParams);
		FLProjectWriter.WriteArrayEventLength(w, 24);

		w.WriteUInt32(0); // 0% min volume
		w.WriteUInt32(12_800); // 100% max volume

		w.WriteUInt32(0);
		w.WriteUInt32(0x100);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
	}
}
