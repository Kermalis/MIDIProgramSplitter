using Kermalis.EndianBinaryIO;

namespace FLP;

public sealed class FLPlaylistTrack
{
	public const float SIZE_MIN = 0f;
	public const float SIZE_DEFAULT = 1f;
	public const float SIZE_MAX = 25.9249992370605f;
	public static FLColor3 DefaultColor => new(72, 81, 86);

	internal readonly ushort Index;
	internal ushort ID => (ushort)(Index + 1);

	public float Size;
	public bool GroupWithAbove;
	/// <summary>Only works if this track is the parent of the group</summary>
	public bool IsGroupCollapsed;
	public string? Name;
	public FLColor3 Color;
	public uint Icon;

	internal FLPlaylistTrack(ushort index)
	{
		Index = index;
		Color = DefaultColor;
		Size = SIZE_DEFAULT;
	}

	internal void Write(EndianBinaryWriter w, FLVersionCompat verCom)
	{
		w.WriteEnum(FLEvent.NewPlaylistTrack);
		FLProjectWriter.WriteArrayEventLength(w, 66);

		w.WriteUInt32(ID);

		w.WriteByte(Color.R);
		w.WriteByte(Color.G);
		w.WriteByte(Color.B);
		w.WriteByte(0);

		w.WriteUInt32(Icon);

		w.WriteByte(1);

		w.WriteSingle(Size);

		// The default height in pixels is 56
		// If I "Lock to this size", this becomes 0x38 (56) instead of -16 or -1
		// If I manually resize it, this becomes -56 and Size (above) changes
		// Even if I reset the size to 100%, this stays -56 instead of going back to the weird value
		w.WriteInt32(Index <= 0x20 ? -16 : -1); // TODO: Why? 

		w.WriteByte(0);
		w.WriteByte(0); // Performance Motion
		w.WriteInt16(0);

		w.WriteByte(0);
		w.WriteByte(0); // Performance Press
		w.WriteInt16(0);

		w.WriteByte(0);
		w.WriteByte(5); // Performance Trigger Sync (4 beats)
		w.WriteInt16(0);

		w.WriteByte(0);
		w.WriteBoolean(false); // Performance Queue
		w.WriteInt16(0);

		w.WriteByte(0);
		w.WriteBoolean(true); // Performance Tolerant
		w.WriteInt16(0);

		w.WriteByte(0);
		w.WriteByte(0); // Performance Position Sync
		w.WriteInt16(0);

		w.WriteByte(0);
		w.WriteBoolean(GroupWithAbove);
		w.WriteInt16(0);

		w.WriteInt32(0); // Was 1 in "track mode - audio track" and 3 in "track mode - instrument track"

		w.WriteInt32(-1); // In audio track mode, it was the insert
		w.WriteInt32(-1); // In instrument track mode, it was the channelID

		w.WriteBoolean(!IsGroupCollapsed);

		w.WriteInt32(0); // Track Mode Instrument Track Options

		if (Name is not null)
		{
			FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.PlaylistTrackName, Name + '\0');
		}

		// TODO: Is Unk_43 before or after Name?
		if (verCom == FLVersionCompat.V21_0_3__B3517)
		{
			FLProjectWriter.Write8BitEvent(w, FLEvent.Unk_43, 0);
		}
	}
}
