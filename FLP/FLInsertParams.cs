using Kermalis.EndianBinaryIO;

namespace FLP;

internal struct FLInsertParams
{
	private const InsertFlags MASTER_CURRENT_FLAGS = InsertFlags.Unk_2 | InsertFlags.Unmuted;
	private const InsertFlags INSERT_FLAGS = InsertFlags.Unk_2 | InsertFlags.Unmuted | InsertFlags.DockMiddle;

	public static void Write(EndianBinaryWriter w, bool isMasterOrCurrent)
	{
		w.WriteEnum(FLEvent.InsertParams);
		FLProjectWriter.WriteArrayEventLength(w, 12);

		w.WriteUInt32(0); // Always 0?
		w.WriteEnum(isMasterOrCurrent ? MASTER_CURRENT_FLAGS : INSERT_FLAGS);
		w.WriteUInt16(0); // Always 0?
		w.WriteUInt32(0); // Always 0?
	}
}
