using Kermalis.EndianBinaryIO;

namespace FLP;

public struct FLBasicChannelParams
{
	public const uint KNOB_MIN = 0;
	public const uint KNOB_HALF = 6_400;
	public const uint KNOB_MAX = 12_800; // 0x3200
	public const uint DEFAULT_PAN = KNOB_HALF; // Center
	public const uint DEFAULT_VOL = 10_000; // 78.125%

	internal static void WriteChannel(EndianBinaryWriter w, uint panKnob, uint volKnob, int pitchKnob)
	{
		w.WriteEnum(FLEvent.BasicChannelParams);
		FLProjectWriter.WriteArrayEventLength(w, 24);

		w.WriteUInt32(panKnob);
		w.WriteUInt32(volKnob);
		w.WriteInt32(pitchKnob); // In cents

		w.WriteUInt32(0x100); // Always 0x100?
		w.WriteUInt32(0); // Always 0?
		w.WriteUInt32(0); // Always 0?
	}
	internal static void WriteAutomation(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.BasicChannelParams);
		FLProjectWriter.WriteArrayEventLength(w, 24);

		w.WriteUInt32(0); // 0% min volume
		w.WriteUInt32(12_800); // 100% max volume
		w.WriteInt32(0);

		w.WriteUInt32(0x100);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
	}
}
