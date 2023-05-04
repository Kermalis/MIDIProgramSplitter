using FLP;
using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class NewTrackPattern
{
	public sealed class Note
	{
		public MIDIEvent On;
		public MIDIEvent Off;

		public Note(MIDIEvent on, MIDIEvent off)
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

	public void AddToFLP(FLProjectWriter w, FLChannel channel, FLPlaylistTrack pTrack, string? name)
	{
		// Randomize pattern colors for fun
		FLPattern p = w.CreatePattern();
		p.Color = FLColor3.GetRandom();
		p.Name = name;

		uint startTick = (uint)Notes[0].On.Ticks;
		uint endTick = (uint)Notes[Notes.Count - 1].Off.Ticks;

		foreach (Note note in Notes)
		{
			MIDIEvent noteOnE = note.On;
			var noteOn = (NoteOnMessage)noteOnE.Message;
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

		w.Arrangements[0].AddToPlaylist(p, startTick, endTick - startTick, pTrack);
	}
}
