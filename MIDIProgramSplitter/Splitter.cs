using Kermalis.MIDI;
using MIDIProgramSplitter.FLP;
using System;
using System.Collections.Generic;
using System.IO;

namespace MIDIProgramSplitter;

internal sealed class Splitter
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
			t.AddNewTracks(midi);
		}

		using (FileStream fs = File.Create(outFile))
		{
			midi.Save(fs);
		}
	}

	public void Test_WriteFLP()
	{
		const string OUT = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TestOUT.flp";

		using (FileStream s = File.Create(OUT))
		{
			var w = new FLProjectWriter(ppqn: _inMIDI.HeaderChunk.TimeDivision.PPQN_TicksPerQuarterNote);

			FLP_ReadMetaTrack(w, out decimal tempo, out byte timeSigNum, out byte timeSigDenom);
			w.CurrentTempo = tempo;
			w.TimeSigNumerator = timeSigNum;
			w.TimeSigDenominator = timeSigDenom;

			foreach (TrackData t in _splitTracks)
			{
				t.FLP_HandleTrack(w, _maxTicks);
			}

			w.Write(s);
		}
	}
	private void FLP_ReadMetaTrack(FLProjectWriter w, out decimal tempo, out byte timeSigNum, out byte timeSigDenom)
	{
		const int DEFAULT_MIDI_TEMPO = 120;

		tempo = DEFAULT_MIDI_TEMPO;
		MIDIEvent? firstTempo = null;
		FLAutomation? tempoAuto = null;
		timeSigNum = 4;
		timeSigDenom = 4;
		MIDIEvent? firstTimeSig = null;
		bool createdFirstTimeSigMarker = false;

		for (MIDIEvent? e = _metaTrack.First; e is not null; e = e.Next)
		{
			var m = (MetaMessage)e.Message;
			switch (m.Type)
			{
				case MetaMessageType.Tempo:
				{
					FLP_HandleTempo(e, m, w, ref tempo, ref firstTempo, ref tempoAuto);
					break;
				}
				case MetaMessageType.TimeSignature:
				{
					FLP_HandleTimeSig(e, m, w, ref timeSigNum, ref timeSigDenom, ref firstTimeSig, ref createdFirstTimeSigMarker);
					break;
				}
			}
		}

		tempoAuto?.PadTempoPoints(_maxTicks, DEFAULT_MIDI_TEMPO);
	}
	private void FLP_HandleTempo(MIDIEvent e, MetaMessage m, FLProjectWriter w,
		ref decimal tempo, ref MIDIEvent? firstTempo, ref FLAutomation? tempoAuto)
	{
		m.ReadTempoMessage(out _, out decimal bpm);
		if (firstTempo is null)
		{
			tempo = bpm;
			firstTempo = e;
			return;
		}

		// This is the 2nd or after change. 2nd will create the autoclip
		if (tempoAuto is null)
		{
			tempoAuto = new FLAutomation("Tempo", FLAutomation.MyType.Tempo, null);
			w.Automations.Add(tempoAuto);
			w.AddToPlaylist(tempoAuto, 0, _maxTicks, w.PlaylistTracks[200]); // TODO: track

			tempoAuto.AddTempoPoint((uint)firstTempo.Ticks, tempo);
		}
		tempoAuto.AddTempoPoint((uint)e.Ticks, bpm);
	}
	private static void FLP_HandleTimeSig(MIDIEvent e, MetaMessage m, FLProjectWriter w,
		ref byte timeSigNum, ref byte timeSigDenom, ref MIDIEvent? firstTimeSig, ref bool createdFirstTimeSigMarker)
	{
		m.ReadTimeSignatureMessage(out byte num, out byte denom, out _, out _);
		if (firstTimeSig is null)
		{
			timeSigNum = num;
			timeSigDenom = denom;
			firstTimeSig = e;
			return;
		}

		// This is the 2nd or after change. 2nd will create the marker for #1 and #2
		if (!createdFirstTimeSigMarker)
		{
			createdFirstTimeSigMarker = true;
			w.AddTimeSigMarker((uint)firstTimeSig.Ticks, timeSigNum, timeSigDenom);
		}
		w.AddTimeSigMarker((uint)e.Ticks, num, denom);
	}
}
