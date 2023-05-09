using Kermalis.EndianBinaryIO;

namespace FLP;

internal struct FLNewPlugin
{
	// VST Notes: https://github.com/Kaydax/FLParser/blob/2f48809bbf8f31c4ecf93051f5f1fa86a84b5468/ProjectParser.cs#L601

	public static void WriteMIDIOut(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.NewPlugin);
		FLProjectWriter.WriteArrayEventLength(w, 52);

		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(2);
		w.WriteUInt32(0);
		w.WriteUInt32(0b1_0101_0100); // 0x154 (340) = That 1 at the left seems to be "is open"? But these are closed...
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(4); // Selection/pos related. Was 0x0112 when selected, then 0x0004 when deselected in the same pos. Also saw it 0x12B
		w.WriteUInt32(4); // Saw 0x15D
		w.WriteUInt32(0);
		w.WriteUInt32(0);
	}
	public static void WriteAutomation(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.NewPlugin);
		FLProjectWriter.WriteArrayEventLength(w, 52);

		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteInt32(-1);
		w.WriteUInt32(0);
		w.WriteUInt32(0b0_0101_0100); // 0x54 (84)
		w.WriteUInt32(5);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(6);
		w.WriteUInt32(6);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
	}
	public static void WriteFruityLSD(EndianBinaryWriter w, byte insertIndex)
	{
		w.WriteEnum(FLEvent.NewPlugin);
		FLProjectWriter.WriteArrayEventLength(w, 52);

		w.WriteUInt32(insertIndex);
		w.WriteUInt32(0);
		w.WriteUInt32(2);
		w.WriteUInt32(0);
		w.WriteUInt32(0b1_0100_0100); // 0x144 (324)
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0x3D3); // 979
		w.WriteUInt32(0x337); // 823
		w.WriteUInt32(0);
		w.WriteUInt32(0);
	}
}
