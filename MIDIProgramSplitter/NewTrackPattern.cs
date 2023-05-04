using Kermalis.MIDI;
using MIDIProgramSplitter.FLP;
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

	public void AddToFLP(FLProjectWriter w, ushort chanID, FLPlaylistTrack pTrack, string? name)
	{
		// Randomize pattern colors for fun
		var p = new FLPattern
		{
			Color = FLColor3.GetRandom(),
			Name = name,
		};
		w.Patterns.Add(p);

		uint startTick = (uint)Notes[0].On.Ticks;
		uint endTick = (uint)Notes[Notes.Count - 1].Off.Ticks;

		foreach (Note note in Notes)
		{
			MIDIEvent noteOnE = note.On;
			var noteOn = (NoteOnMessage)noteOnE.Message;
			uint onAbsoluteTick = (uint)noteOnE.Ticks;
			uint offAbsoluteTick = (uint)note.Off.Ticks;

			p.Notes.Add(new FLPatternNote
			{
				Channel = chanID,
				AbsoluteTick = onAbsoluteTick - startTick,
				DurationTicks = offAbsoluteTick - onAbsoluteTick,
				Key = (byte)noteOn.Note,
				Velocity = noteOn.Velocity,
			});
		}

		w.AddToPlaylist(p, startTick, endTick - startTick, pTrack);
	}
}
