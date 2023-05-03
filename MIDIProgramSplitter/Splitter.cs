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
		if (format != MIDIFormat.Format1)
		{
			throw new Exception("MIDI is not Format1. Only Format1 is supported for now.");
		}

		ushort numTracks = _inMIDI.HeaderChunk.NumTracks;
		Console.WriteLine("MIDI Tracks: " + numTracks);
		if (numTracks < 2)
		{
			throw new Exception("MIDI has too few tracks (expected at least 2)");
		}

		TimeDivisionValue timeDiv = _inMIDI.HeaderChunk.TimeDivision;
		Console.WriteLine("MIDI TimeDiv: " + timeDiv);

		_splitTracks = new List<TrackData>();

		byte trackIndex = 0;
		foreach (MIDITrackChunk track in _inMIDI.EnumerateTrackChunks())
		{
			// Meta track special case
			if (trackIndex == 0/* && _inMIDI.HeaderChunk.Format == MIDIFormat.Format1*/)
			{
				// Check first. The Colress MIDI I have has the first track with notes and tempo/timesig info, which is wrong.
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

	private static bool TrackHasNonMetaMessages(MIDITrackChunk t)
	{
		for (MIDIEvent? e = t.First; e is not null; e = e.Next)
		{
			if (e.Message is not MetaMessage)
			{
				return true;
			}
		}
		return false;
	}

	public void SaveMIDI(string outFile)
	{
		var midi = new MIDIFile(MIDIFormat.Format1, _inMIDI.HeaderChunk.TimeDivision, 1);

		Console.WriteLine("Copying Format1 meta track");
		midi.AddChunk(_metaTrack);

		foreach (TrackData t in _splitTracks)
		{
			t.MIDI_AddNewTracks(midi);
		}

		using (FileStream fs = File.Create(outFile))
		{
			midi.Save(fs);
		}
	}
}
