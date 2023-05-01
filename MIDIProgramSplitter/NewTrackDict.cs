using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class NewTrackDict
{
	public readonly Dictionary<MIDIProgram, NewTrack> Dict;

	public NewTrackDict(byte trackIndex, byte channel, HashSet<MIDIProgram> usedVoices, List<MIDIEvent> programEvents, uint maxTicks)
	{
		Dict = new Dictionary<MIDIProgram, NewTrack>();

		// Create a track for each used voice
		foreach (MIDIProgram voice in usedVoices)
		{
			// Keep track of where this is used
			var list = new List<(uint, uint)>();
			for (int i = 0; i < programEvents.Count; i++)
			{
				MIDIEvent e = programEvents[i];
				var m = (ProgramChangeMessage)e.Message;
				if (m.Program == voice)
				{
					uint cur = (uint)e.Ticks;
					uint next = i == programEvents.Count - 1 ? maxTicks : (uint)programEvents[i + 1].Ticks;
					list.Add((cur, next - cur));
				}
			}

			Dict.Add(voice, new NewTrack(voice, list, string.Format("T{0}C{1} {2}", trackIndex + 1, channel + 1, voice)));
		}
	}

	/// <summary>If there is a program change in the original track, but no NoteOn use it, that voice is ignored.</summary>
	public bool VoiceIsUsed(MIDIProgram voice)
	{
		return Dict.ContainsKey(voice);
	}

	public void InsertEventIntoNewTrack(MIDIEvent e, MIDIProgram voice)
	{
		Dict[voice].Track.InsertMessage(e.Ticks, e.Message);
	}
	public void InsertEventIntoAllNewTracks(MIDIEvent e)
	{
		foreach (NewTrack t in Dict.Values)
		{
			t.Track.InsertMessage(e.Ticks, e.Message);
		}
	}
}
