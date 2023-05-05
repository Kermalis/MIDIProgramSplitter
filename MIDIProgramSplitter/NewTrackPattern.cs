using FLP;
using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class NewTrackPattern
{
	public sealed class Note
	{
		public MIDIEvent<NoteOnMessage> On;
		public IMIDIEvent Off;

		public Note(MIDIEvent<NoteOnMessage> on, IMIDIEvent off)
		{
			On = on;
			Off = off;
		}
	}

	public readonly List<Note> Notes;

	public NewTrackPattern()
	{
		Notes = new List<Note>();
	}

	public void AddToFLP(FLPSaver saver, FLChannel channel, FLPlaylistTrack pTrack, string? name)
	{
		FLPattern p = saver.FLP.CreatePattern();
		p.Color = saver.Options.GetPatternColor();
		p.Name = name;

		uint startTick = (uint)Notes[0].On.Ticks; // TODO: Some quantization?
		uint endTick = (uint)Notes[Notes.Count - 1].Off.Ticks;

		foreach (Note note in Notes)
		{
			MIDIEvent<NoteOnMessage> noteOnE = note.On;
			NoteOnMessage noteOn = noteOnE.Msg;
			uint onAbsoluteTick = (uint)noteOnE.Ticks;
			uint offAbsoluteTick = (uint)note.Off.Ticks;

			p.Notes.Add(new FLPatternNote(channel)
			{
				AbsoluteTick = onAbsoluteTick - startTick,
				DurationTicks = offAbsoluteTick - onAbsoluteTick,
				Key = noteOn.Note,
				Velocity = noteOn.Velocity,
			});
		}

		saver.FLP.Arrangements[0].AddToPlaylist(p, startTick, endTick - startTick, pTrack);
	}
}
