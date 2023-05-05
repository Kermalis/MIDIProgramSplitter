using FLP;
using Kermalis.MIDI;
using System.IO;

namespace MIDIProgramSplitter;

partial class Splitter
{
	public void SaveFLP(string outFile, string dlsPath)
	{
		using (FileStream s = File.Create(outFile))
		{
			var w = new FLProjectWriter
			{
				PPQN = _inMIDI.HeaderChunk.TimeDivision.PPQN_TicksPerQuarterNote,
				TEMP_DLSPath = dlsPath,
			};

			FLChannelFilter? autoFilter = null;
			int automationTrackIndex = _inMIDI.HeaderChunk.NumTracks - 1; // Meta track won't use one

			FLP_AddMetaTrackEvents(w, ref autoFilter, ref automationTrackIndex, out decimal tempo, out byte timeSigNum, out byte timeSigDenom);
			w.CurrentTempo = tempo;
			w.TimeSigNumerator = timeSigNum;
			w.TimeSigDenominator = timeSigDenom;

			foreach (TrackData t in _splitTracks)
			{
				t.FLP_AddNewTracks(w, _maxTicks, ref automationTrackIndex);
			}

			w.Write(s);
		}
	}

	private void FLP_AddMetaTrackEvents(FLProjectWriter w, ref FLChannelFilter? autoFilter, ref int automationTrackIndex,
		out decimal tempo, out byte timeSigNum, out byte timeSigDenom)
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
					FLP_HandleTempo(e, m, w, ref autoFilter, ref automationTrackIndex, ref tempo, ref firstTempo, ref tempoAuto);
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
		ref FLChannelFilter? autoFilter, ref int automationTrackIndex, ref decimal tempo, ref MIDIEvent? firstTempo, ref FLAutomation? tempoAuto)
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
			autoFilter ??= w.CreateAutomationFilter();
			tempoAuto = w.CreateTempoAutomation("Tempo", autoFilter);

			FLArrangement arrang = w.Arrangements[0];
			FLPlaylistTrack track = arrang.PlaylistTracks[automationTrackIndex++];
			track.Size = 1f / 3;
			arrang.AddToPlaylist(tempoAuto, 0, _maxTicks, track);

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

		FLArrangement arrang = w.Arrangements[0];

		// This is the 2nd or after change. 2nd will create the marker for #1 and #2
		if (!createdFirstTimeSigMarker)
		{
			createdFirstTimeSigMarker = true;
			arrang.AddTimeSigMarker((uint)firstTimeSig.Ticks, timeSigNum, timeSigDenom);
		}
		arrang.AddTimeSigMarker((uint)e.Ticks, num, denom);
	}
}
