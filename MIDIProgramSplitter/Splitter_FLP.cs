using FLP;
using Kermalis.MIDI;
using System.IO;

namespace MIDIProgramSplitter;

partial class Splitter
{
	// TODO: Preserve midi track names in FLP
	public void SaveFLP(Stream s, FLPSaveOptions options)
	{
		options.Validate();

		var saver = new FLPSaver(options);

		saver.MaxTicks = _maxTicks;
		saver.FLP.PPQN = _inMIDI.HeaderChunk.TimeDivision.PPQN_TicksPerQuarterNote;

		// Place automations under the MIDItrack patterns. MIDI metatrack doesn't use a playlist track
		saver.AutomationTrackIndex = _inMIDI.HeaderChunk.NumTracks - 1;

		for (int i = 0; i < saver.AutomationTrackIndex; i++)
		{
			FLInsert ins = saver.FLP.Inserts[i + 1]; // Skip master insert
			ins.Color = saver.Options.GetInsertColor();
			ins.Name = string.Format("T{0:D2}", i + 1); // Skip meta track
			ins.FruityLSD = new FLInsert.FLFruityLSDOptions((byte)i, options.DLSPath, FLInsert.FLFruityLSDOptions.GetDefaultColor(options.FLVersionCompat));
		}

		FLP_AddMetaTrackEvents(saver);

		foreach (TrackData t in _splitTracks)
		{
			t.FLP_AddNewTracks(saver);
		}

		saver.FLP.Write(s);
	}

	private void FLP_AddMetaTrackEvents(FLPSaver saver)
	{
		// TODO: Markers for other text messages?
		for (IMIDIEvent? ev = _metaTrack.First; ev is not null; ev = ev.Next)
		{
			var e = (IMIDIEvent<MetaMessage>)ev;
			switch (e.Msg.Type)
			{
				case MetaMessageType.Tempo:
				{
					saver.HandleTempo(e);
					break;
				}
				case MetaMessageType.TimeSignature:
				{
					saver.HandleTimeSig(e);
					break;
				}
			}
		}

		saver.TempoAuto?.PadTempoPoints(_maxTicks, 120);
	}
}
