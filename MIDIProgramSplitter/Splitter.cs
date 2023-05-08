using Kermalis.MIDI;
using System;
using System.Collections.Generic;
using System.IO;

namespace MIDIProgramSplitter;

public sealed partial class Splitter
{
	private readonly MIDIFile _inMIDI;
	private readonly uint _maxTicks;
	private readonly MIDITrackChunk _metaTrack;
	private readonly List<TrackData> _splitTracks;

	public Splitter(MIDIFile m)
	{
		_inMIDI = m;
		_metaTrack = null!;

		Console.WriteLine();

		MIDIFormat format = _inMIDI.HeaderChunk.Format;
		Console.WriteLine("MIDI Format: " + format);
		ushort numTracks = _inMIDI.HeaderChunk.NumTracks;
		Console.WriteLine("MIDI Tracks: " + numTracks);
		TimeDivisionValue timeDiv = _inMIDI.HeaderChunk.TimeDivision;
		Console.WriteLine("MIDI TimeDiv: " + timeDiv);

		if (format == MIDIFormat.Format0)
		{
			// NumTracks already verified by MIDIFile constructor if this is Format0
			Console.WriteLine("Attempting to convert from Format0 to Format1...");
			_inMIDI = ConvertFormat0ToFormat1(_inMIDI);
			Console.WriteLine("Successfully converted to Format1!");
		}
		else if (format == MIDIFormat.Format1)
		{
			// TODO: 2 tracks if we have a proper one. May have an improper one with just 1
			// TODO: Are there proper ones with no meta track (no meta messages?)
			if (numTracks < 2)
			{
				throw new Exception("MIDI has too few tracks (expected at least 2 for Format1)");
			}
		}
		else
		{
			throw new Exception($"MIDI is not Format 0 or 1. Format not supported: {format}");
		}

		_splitTracks = new List<TrackData>();

		byte trackIndex = 0;
		foreach (MIDITrackChunk track in _inMIDI.EnumerateTrackChunks())
		{
			// Meta track special case
			if (trackIndex == 0)
			{
				// Check first. vgmtrans puts the first track with notes and tempo/timesig info, which is wrong.
				// TODO: Can we fix it ourselves easily? I keep re-saving them with Anvil Studio to fix
				if (TrackHasNonMetaMessages(track))
				{
					throw new Exception($"Format1 MIDI must have a {nameof(MetaMessage)} track as the first track. This MIDI included other messages in the first track, which is invalid");
				}
				_metaTrack = track;
			}
			else
			{
				var td = new TrackData(trackIndex, track);
				td.SplitTrack();
				_splitTracks.Add(td);
			}
			trackIndex++;

			if (track.NumTicks > _maxTicks)
			{
				_maxTicks = (uint)track.NumTicks;
			}
		}
	}

	private static MIDIFile ConvertFormat0ToFormat1(MIDIFile inMIDI)
	{
		var newMIDI = new MIDIFile(MIDIFormat.Format1, inMIDI.HeaderChunk.TimeDivision, 2);
		var metaTrack = new MIDITrackChunk();
		newMIDI.AddChunk(metaTrack);

		// TODO: Always create meta track, even if there are no meta events?
		MIDITrackChunk track = GetFirstTrack(inMIDI);

		// Channels are the key
		var newTracks = new Dictionary<byte, MIDITrackChunk>();

		for (IMIDIEvent? ev = track.First; ev is not null; ev = ev.Next)
		{
			ConvertFormat0ToFormat1_HandleEvent(ev, newMIDI, metaTrack, newTracks);
		}

		return newMIDI;
	}
	private static void ConvertFormat0ToFormat1_HandleEvent(IMIDIEvent ev,
		MIDIFile newMIDI, MIDITrackChunk metaTrack, Dictionary<byte, MIDITrackChunk> newTracks)
	{
		if (ev.Msg is IMIDIChannelMessage m)
		{
			if (!newTracks.TryGetValue(m.Channel, out MIDITrackChunk? chanTrack))
			{
				Console.WriteLine("Discovered channel {0}!", m.Channel);
				chanTrack = new MIDITrackChunk();
				newMIDI.AddChunk(chanTrack);
				newTracks.Add(m.Channel, chanTrack);
			}
			chanTrack.InsertMessage(ev.Ticks, ev.Msg);
			return;
		}
		if (ev is IMIDIEvent<MetaMessage> e)
		{
			switch (e.Msg.Type)
			{
				case MetaMessageType.Marker:
				case MetaMessageType.Tempo:
				case MetaMessageType.TimeSignature:
				{
					metaTrack.InsertMessage(ev.Ticks, e.Msg);
					return;
				}
				case MetaMessageType.EndOfTrack:
				{
					metaTrack.InsertMessage(ev.Ticks, e.Msg);
					foreach (MIDITrackChunk t in newTracks.Values)
					{
						t.InsertMessage(ev.Ticks, e.Msg);
					}
					return;
				}
			}
		}

		Console.WriteLine("Skipping event: " + ev.Msg);
		;
	}

	private static MIDITrackChunk GetFirstTrack(MIDIFile midi)
	{
		MIDITrackChunk track = null!;
		foreach (MIDITrackChunk t in midi.EnumerateTrackChunks())
		{
			track = t;
		}
		return track;
	}
	private static bool TrackHasNonMetaMessages(MIDITrackChunk t)
	{
		for (IMIDIEvent? e = t.First; e is not null; e = e.Next)
		{
			if (e is not IMIDIEvent<MetaMessage>)
			{
				return true;
			}
		}
		return false;
	}

	public void SaveMIDI(Stream s)
	{
		var midi = new MIDIFile(MIDIFormat.Format1, _inMIDI.HeaderChunk.TimeDivision, 1);

		midi.AddChunk(_metaTrack);
		foreach (TrackData t in _splitTracks)
		{
			t.MIDI_AddNewTracks(midi);
		}

		midi.Save(s);
	}
}
