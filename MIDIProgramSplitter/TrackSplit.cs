using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class TrackSplit
{
	private readonly Dictionary<MIDIProgram, MIDITrackChunk> _dict;

	public TrackSplit(byte trackIndex, byte channel, HashSet<MIDIProgram> usedVoices, OutMIDI outMIDI)
	{
		_dict = new Dictionary<MIDIProgram, MIDITrackChunk>();

		// Create a track for each used voice
		foreach (MIDIProgram voice in usedVoices)
		{
			MIDITrackChunk t = outMIDI.Create();
			_dict.Add(voice, t);

			t.InsertMessage(0, MetaMessage.CreateTextMessage(MetaMessageType.TrackName, string.Format("T{0}C{1} {2}", trackIndex + 1, channel + 1, voice)));
			// Cannot set voice here since it'd mess up the entire channel
		}
	}

	/// <summary>If there is a program change in the original track, but no NoteOn use it, that voice is ignored.</summary>
	public bool VoiceIsUsed(MIDIProgram voice)
	{
		return _dict.ContainsKey(voice);
	}

	public void InsertEventIntoNewTrack(MIDIEvent e, MIDIProgram voice)
	{
		_dict[voice].InsertMessage(e.Ticks, e.Message);
	}
	public void InsertEventIntoAllNewTracks(MIDIEvent e)
	{
		foreach (MIDITrackChunk t in _dict.Values)
		{
			t.InsertMessage(e.Ticks, e.Message);
		}
	}
}
