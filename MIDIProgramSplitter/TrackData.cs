using Kermalis.MIDI;
using System;
using System.Collections.Generic;
using System.IO;

namespace MIDIProgramSplitter;

// Assumes a format1 track
internal sealed partial class TrackData
{
	private struct PlayingNote
	{
		public MIDIProgram Program;
		public NewTrackPattern Pat;
		public MIDIEvent NoteOnE;
	}

	private readonly byte _trackIndex;
	private readonly MIDITrackChunk _inTrack;
	private readonly byte _trackChannel;

	/// <summary>Each HashSet keeps track of the programs that have NoteOn</summary>
	private readonly HashSet<MIDIProgram> _usedPrograms;
	/// <summary>Used while iterating the events</summary>
	private MIDIProgram _curProgram;
	private readonly List<PlayingNote> _playingNotes;
	/// <summary>The new tracks created</summary>
	private readonly NewTrackDict? _newTracks; // Allow null I guess since you could be copying automations for later with no notes

	private readonly List<MIDIEvent> _volEvents;
	private readonly List<MIDIEvent> _panEvents;
	private readonly List<MIDIEvent> _pitchEvents;
	private readonly List<MIDIEvent> _programEvents;

	/// <summary>Scavenges <paramref name="inTrack"/> for events and splits them into new tracks</summary>
	public TrackData(byte trackIndex, MIDITrackChunk inTrack)
	{
		_trackIndex = trackIndex;
		_inTrack = inTrack;

		_usedPrograms = new HashSet<MIDIProgram>();
		_playingNotes = new List<PlayingNote>();
		_volEvents = new List<MIDIEvent>();
		_panEvents = new List<MIDIEvent>();
		_pitchEvents = new List<MIDIEvent>();
		_programEvents = new List<MIDIEvent>();

		_trackChannel = GatherTrackInfoFirstPass();

		_newTracks = _usedPrograms.Count == 0 ? null : new NewTrackDict(trackIndex, _trackChannel, _usedPrograms);
	}

	/// <summary>Program changes with no NoteOn are not recorded.</summary>
	private byte GatherTrackInfoFirstPass()
	{
		byte chan = byte.MaxValue;

		for (MIDIEvent? e = _inTrack.First; e is not null; e = e.Next)
		{
			switch (e.Message)
			{
				case ProgramChangeMessage m:
				{
					CheckChannel(m, ref chan);
					_curProgram = m.Program;
					_programEvents.Add(e);
					break;
				}
				case NoteOnMessage m:
				{
					CheckChannel(m, ref chan);
					if (m.Velocity != 0)
					{
						_usedPrograms.Add(_curProgram);
					}
					break;
				}
				case PitchBendMessage m:
				{
					CheckChannel(m, ref chan);
					_pitchEvents.Add(e);
					break;
				}
				case ControllerMessage m:
				{
					CheckChannel(m, ref chan);
					switch (m.Controller)
					{
						case ControllerType.ChannelVolume: _volEvents.Add(e); break;
						case ControllerType.Pan: _panEvents.Add(e); break;
					}
					break;
				}
				case IMIDIChannelMessage m: // NoteOff, ChannelPressure, PolyphonicPressure
				{
					CheckChannel(m, ref chan);
					break;
				}
			}
		}

		if (chan == byte.MaxValue)
		{
			// TODO: Allow, but skip track
			throw new InvalidDataException($"Track {_trackIndex} had no MIDI channel events...");
		}
		return chan;
	}
	private void CheckChannel(IMIDIChannelMessage m, ref byte chan)
	{
		if (chan == byte.MaxValue)
		{
			chan = m.Channel; // This is the first event with the channel
		}
		else if (chan != m.Channel)
		{
			throw new InvalidDataException($"Track {_trackIndex} contains multiple MIDI channels. Only 1 supported");
		}
	}

	/// <summary>Iterates all messages in <see cref="_inTrack"/> and copies/splits them to the new tracks</summary>
	public void SplitTrack()
	{
		if (_newTracks is null)
		{
			Console.WriteLine("Skipping track {0} because it has no notes...", _trackIndex);
			return;
		}

		NewTrackDict ts = _newTracks;
		_curProgram = 0;
		NewTrackPattern? curPattern = null;

		for (MIDIEvent? e = _inTrack.First; e is not null; e = e.Next)
		{
			if (SplitTrack_SpecialMessage(e, ts, ref curPattern))
			{
				_newTracks.InsertEventIntoAllNewTracks(e);
			}
		}

		if (_playingNotes.Count != 0)
		{
			throw new InvalidDataException($"Track {_trackIndex}: NoteOn and NoteOff count mismatch...");
		}
	}
	/// <summary>Returns false if this event should not be added to the new tracks</summary>
	private bool SplitTrack_SpecialMessage(MIDIEvent e, NewTrackDict ts, ref NewTrackPattern? curPattern)
	{
		MIDIMessage msg = e.Message;
		switch (msg)
		{
			case ProgramChangeMessage m:
			{
				_curProgram = m.Program;
				curPattern = null;
				if (!ts.VoiceIsUsed(_curProgram))
				{
					return false; // Ignore this program change in that case
				}
				break; // Add to all new tracks on this channel
			}
			case NoteOnMessage m:
			{
				if (m.Velocity == 0)
				{
					SplitTrack_StopPlayingNote(e, ts, m.Note);
				}
				else
				{
					if (curPattern is null)
					{
						curPattern = new NewTrackPattern();
						ts.Dict[_curProgram].Patterns.Add(curPattern);
					}
					_playingNotes.Add(new PlayingNote
					{
						Program = _curProgram,
						Pat = curPattern,
						NoteOnE = e
					});

					ts.InsertEventIntoNewTrack(e, _curProgram);
				}
				return false; // Only add to correct new track
			}
			case NoteOffMessage m:
			{
				// TODO: NoteOff velocity? It doesn't always match. NoteOn was 111 and NoteOff was 64
				SplitTrack_StopPlayingNote(e, ts, m.Note);
				return false; // Only add to correct new track
			}
			case MetaMessage m:
			{
				Console.WriteLine("Track {0}: {1}", _trackIndex, m);

				if (m.Type == MetaMessageType.TrackName)
				{
					return false; // Ignore these
				}
				break; // Add to all new tracks
			}
		}

		return true;
	}
	private void SplitTrack_StopPlayingNote(MIDIEvent e, NewTrackDict ts, MIDINote n)
	{
		// Try to catch the oldest first
		for (int i = 0; i < _playingNotes.Count; i++)
		{
			PlayingNote p = _playingNotes[i];
			var noteOn = (NoteOnMessage)p.NoteOnE.Message;
			if (noteOn.Note == n) // Check channel also if not Format1
			{
				_playingNotes.RemoveAt(i);
				ts.InsertEventIntoNewTrack(e, p.Program);
				p.Pat.Notes.Add(new NewTrackPattern.Note(p.NoteOnE, e));
				return;
			}
		}
		throw new InvalidDataException($"Track {_trackIndex}: NoteOff without a prior NoteOn...");
	}

	public void MIDI_AddNewTracks(MIDIFile outMIDI)
	{
		if (_newTracks is null)
		{
			return;
		}

		foreach (NewTrack newT in _newTracks.Dict.Values)
		{
			outMIDI.AddChunk(newT.Track);
		}
	}
}
