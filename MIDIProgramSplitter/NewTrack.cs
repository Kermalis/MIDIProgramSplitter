using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class NewTrack
{
	public readonly MIDIProgram Program;
	public readonly MIDITrackChunk Track;
	public readonly string Name;
	public readonly List<(uint StartTicks, uint DurationTicks)> PatternTicksList;
	// TODO: Include patterns of notes here directly

	public NewTrack(MIDIProgram p, List<(uint, uint)> patternTicksList, string name)
	{
		Program = p;
		PatternTicksList = patternTicksList;
		Name = name;
		Track = new MIDITrackChunk();

		Track.InsertMessage(0, MetaMessage.CreateTextMessage(MetaMessageType.TrackName, Name));
		// Cannot set voice here since it'd mess up the entire channel
	}
}
