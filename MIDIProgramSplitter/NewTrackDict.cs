using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class NewTrackDict
{
	public readonly Dictionary<MIDIProgram, NewTrack> Dict;

	public NewTrackDict(byte trackIndex, byte channel, HashSet<MIDIProgram> usedVoices)
	{
		Dict = new Dictionary<MIDIProgram, NewTrack>(usedVoices.Count);

		// Create a track for each used voice
		foreach (MIDIProgram voice in usedVoices)
		{
			Dict.Add(voice, new NewTrack(voice, string.Format("T{0}C{1} {2}", trackIndex + 1, channel + 1, voice)));
		}
	}

	/// <summary>If there is a program change in the original track, but no NoteOn use it, that voice is ignored.</summary>
	public bool VoiceIsUsed(MIDIProgram voice)
	{
		return Dict.ContainsKey(voice);
	}

	public void InsertEventIntoNewTrack(IMIDIEvent e, MIDIProgram voice)
	{
		Dict[voice].Track.InsertMessage(e.Ticks, e.Msg);
	}
	public void InsertEventIntoAllNewTracks(IMIDIEvent e)
	{
		foreach (NewTrack t in Dict.Values)
		{
			t.Track.InsertMessage(e.Ticks, e.Msg);
		}
	}
}
