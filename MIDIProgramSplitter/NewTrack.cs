using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class NewTrack
{
	public readonly MIDIProgram Program;
	public readonly MIDITrackChunk Track;
	public readonly string Name;
	public readonly List<NewTrackPattern> Patterns;

	public NewTrack(MIDIProgram p, string name)
	{
		Program = p;
		Name = name;
		Track = new MIDITrackChunk();
		Patterns = new List<NewTrackPattern>();

		Track.InsertMessage(0, MetaMessage.CreateTextMessage(MetaMessageType.TrackName, Name));
		// Cannot set voice here since it'd mess up the entire channel
	}
}
