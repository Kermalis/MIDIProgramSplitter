using Kermalis.EndianBinaryIO;
using System;

namespace FLP;

public sealed class FLPlaylistItem
{
	internal const int LEN = 32;

	public uint AbsoluteTick;
	public FLPattern? Pattern;
	public FLAutomation? Automation;
	public uint DurationTicks;
	public FLPlaylistTrack PlaylistTrack;

	public FLPlaylistItem(uint tick, FLPattern pattern, uint duration, FLPlaylistTrack track)
	{
		AbsoluteTick = tick;
		Pattern = pattern;
		DurationTicks = duration;
		PlaylistTrack = track;
	}
	public FLPlaylistItem(uint tick, FLAutomation a, uint duration, FLPlaylistTrack track)
	{
		AbsoluteTick = tick;
		Automation = a;
		DurationTicks = duration;
		PlaylistTrack = track;
	}

	internal void Write(EndianBinaryWriter w)
	{
		w.WriteUInt32(AbsoluteTick);

		w.WriteUInt16(0x5000);
		if (Automation is not null)
		{
			w.WriteUInt16(Automation.Index);
		}
		else if (Pattern is not null)
		{
			w.WriteUInt16((ushort)(0x5000 + Pattern.ID));
		}
		else
		{
			throw new InvalidOperationException("Automation and Pattern were null");
		}

		w.WriteUInt32(DurationTicks);

		w.WriteUInt16((ushort)(FLArrangement.NUM_PLAYLIST_TRACKS - PlaylistTrack.ID));
		w.WriteUInt16(0);

		w.WriteByte(0x78); // 120
		w.WriteByte(0);
		w.WriteByte(0x40); // 64
		w.WriteByte(0); // Flags: 0x80 if selected, 0x00 if deselected, 0x20 if disabled and deselected, 0xA0 if disabled and selected

		w.WriteByte(0x40); // 64
		w.WriteByte(0x64); // 100
		w.WriteUInt16(0x8080);

		if (Automation is not null)
		{
			w.WriteSingle(-1f);
			w.WriteSingle(-1f);
		}
		else
		{
			// Both are uint.MaxValue if not manually set to the duration
			w.WriteUInt32(0);
			w.WriteUInt32(DurationTicks);
		}
	}
}
