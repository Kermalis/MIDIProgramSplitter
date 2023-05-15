using FLP;
using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

internal sealed class NewTrackPattern
{
	public sealed class Note
	{
		public IMIDIEvent<NoteOnMessage> On;
		public uint Duration;

		public uint StartTick => (uint)On.Ticks;
		public uint EndTick => StartTick + Duration;

		public Note(IMIDIEvent<NoteOnMessage> on, IMIDIEvent off)
		{
			On = on;
			Duration = (uint)(off.Ticks - on.Ticks);
		}

		public bool Same(Note other, uint patStartTick, uint otherPatStartTick)
		{
			// Pattern ticks are always <= note ticks, so no worry about underflow
			if (StartTick - patStartTick != other.StartTick - otherPatStartTick)
			{
				return false;
			}
			if (Duration != other.Duration)
			{
				return false;
			}
			if (On.Msg.Channel != other.On.Msg.Channel
				|| On.Msg.Note != other.On.Msg.Note
				|| On.Msg.Velocity != other.On.Msg.Velocity)
			{
				return false;
			}
			return true;
		}
	}

	public readonly List<Note> Notes;

	public NewTrackPattern()
	{
		Notes = new List<Note>();
	}

	private uint GetStartTick()
	{
		return Notes[0].StartTick;
	}
	// We can have two notes at the end, where the last one is shorter. So we need to get the correct end
	private uint GetEndTick()
	{
		uint end = 0;
		foreach (Note n in Notes)
		{
			uint t = n.EndTick;
			if (t > end)
			{
				end = t;
			}
		}
		return end;
	}

	public FLPattern AddToFLP(FLPSaver saver, FLChannel channel, FLPlaylistTrack pTrack, MIDIProgram program, string? name)
	{
		FLPattern p = saver.FLP.CreatePattern();
		p.Color = saver.Options.GetPatternColor(program, pTrack);
		p.Name = name;

		uint startTick = GetStartTick(); // TODO: Some quantization?
		uint endTick = GetEndTick();

		foreach (Note note in Notes)
		{
			IMIDIEvent<NoteOnMessage> noteOnE = note.On;
			NoteOnMessage noteOn = noteOnE.Msg;

			p.Notes.Add(new FLPatternNote(channel)
			{
				AbsoluteTick = note.StartTick - startTick,
				DurationTicks = note.Duration,
				Key = noteOn.Note,
				Velocity = noteOn.Velocity,
			});
		}

		PlaceInPlaylist(saver, p, pTrack, startTick, endTick - startTick);
		return p;
	}
	public void AddToFLP_Duplicate(FLPSaver saver, FLPattern p, FLPlaylistTrack pTrack)
	{
		uint startTick = GetStartTick();
		uint endTick = GetEndTick();
		PlaceInPlaylist(saver, p, pTrack, startTick, endTick - startTick);
	}
	private static void PlaceInPlaylist(FLPSaver saver, FLPattern p, FLPlaylistTrack pTrack, uint startTick, uint durationTicks)
	{
		saver.FLP.Arrangements[0].AddToPlaylist(p, startTick, durationTicks, pTrack);
	}

	// TODO: Maybe a pattern starts the same as another but there is more after. Should create a split at that point
	public bool SequenceEqual(NewTrackPattern other)
	{
		if (other.Notes.Count != Notes.Count)
		{
			return false;
		}

		uint startTick = GetStartTick();
		uint otherStartTick = other.GetStartTick();

		for (int i = 0; i < Notes.Count; i++)
		{
			if (!Notes[i].Same(other.Notes[i], startTick, otherStartTick))
			{
				return false;
			}
		}
		return true;
	}
}
