using Kermalis.MIDI;
using System;
using System.IO;

namespace MIDIProgramSplitter;

internal sealed class OutMIDI
{
	private readonly MIDIFile _midi;

	public OutMIDI(MIDIFile inMIDI)
	{
		Console.WriteLine();

		MIDIFormat format = inMIDI.HeaderChunk.Format;
		Console.WriteLine("MIDI Format: " + format);
		if (format != MIDIFormat.Format1)
		{
			throw new Exception("MIDI is not Format1. Only Format1 is supported for now.");
		}

		TimeDivisionValue timeDiv = inMIDI.HeaderChunk.TimeDivision;
		Console.WriteLine("MIDI TimeDiv: " + timeDiv);

		_midi = new MIDIFile(MIDIFormat.Format1, timeDiv, 1);
		// TODO: Only Format1 is supported right now, but the program is mostly built to support all. Just some weird tempo/timesig duplication needs to be fixed

		byte trackIndex = 0;
		foreach (MIDITrackChunk track in inMIDI.EnumerateTrackChunks())
		{
			// Meta track special case
			if (trackIndex == 0 && format == MIDIFormat.Format1)
			{
				// Check first. The Colress MIDI I have has the first track with notes and tempo/timesig info, which is wrong.
				if (TrackHasNonMetaMessages(track))
				{
					throw new Exception($"Format1 MIDI must have a {nameof(MetaMessage)} track as the first track. This MIDI included other messages in the first track, which is invalid");
				}

				Console.WriteLine("Copying Format1 meta track");
				_midi.AddChunk(track);
			}
			else
			{
				var td = new TrackData(trackIndex, track, this);
				td.SplitTrack();
			}
			trackIndex++;
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

	public MIDITrackChunk Create()
	{
		var t = new MIDITrackChunk();
		_midi.AddChunk(t);
		return t;
	}

	public void Save(string outFile)
	{
		using (FileStream fs = File.Create(outFile))
		{
			_midi.Save(fs);
		}
	}
}
