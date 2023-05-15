using Kermalis.EndianBinaryIO;
using System.Collections.Generic;

namespace FLP;

public sealed class FLArrangement
{
	internal const int NUM_PLAYLIST_TRACKS = 500;

	internal ushort Index;

	public string Name;
	public readonly List<FLPlaylistItem> PlaylistItems;
	public readonly List<FLPlaylistMarker> PlaylistMarkers;
	public readonly FLPlaylistTrack[] PlaylistTracks;

	public FLArrangement(string name)
	{
		Name = name;
		PlaylistItems = new List<FLPlaylistItem>();
		PlaylistMarkers = new List<FLPlaylistMarker>();

		PlaylistTracks = new FLPlaylistTrack[NUM_PLAYLIST_TRACKS];
		for (ushort i = 0; i < NUM_PLAYLIST_TRACKS; i++)
		{
			PlaylistTracks[i] = new FLPlaylistTrack(i);
		}
	}

	public void AddToPlaylist(FLPattern p, uint tick, uint duration, FLPlaylistTrack track)
	{
		PlaylistItems.Add(new FLPlaylistItem(tick, p, duration, track));
	}
	public void AddToPlaylist(FLAutomation a, uint tick, uint duration, FLPlaylistTrack track)
	{
		PlaylistItems.Add(new FLPlaylistItem(tick, a, duration, track));
	}
	public void AddTimeSigMarker(uint tick, byte num, byte denom)
	{
		PlaylistMarkers.Add(new FLPlaylistMarker(tick, num + "/" + denom, (num, denom)));
	}

	internal void Write(EndianBinaryWriter w, FLVersionCompat verCom)
	{
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewArrangement, Index);
		FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.PlaylistArrangementName, Name + '\0');
		FLProjectWriter.Write8BitEvent(w, FLEvent.Unk_36, 0);

		// Playlist Items. Must be in order of AbsoluteTick
		PlaylistItems.Sort((p1, p2) => p1.AbsoluteTick.CompareTo(p2.AbsoluteTick));

		w.WriteEnum(FLEvent.PlaylistItems);
		FLProjectWriter.WriteArrayEventLength(w, (uint)PlaylistItems.Count * FLPlaylistItem.LEN);
		foreach (FLPlaylistItem item in PlaylistItems)
		{
			item.Write(w);
		}

		// Playlist Markers
		foreach (FLPlaylistMarker mark in PlaylistMarkers)
		{
			mark.Write(w);
		}

		// Playlist Tracks
		foreach (FLPlaylistTrack track in PlaylistTracks)
		{
			track.Write(w, verCom);
		}
	}
}
