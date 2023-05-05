﻿using Kermalis.MIDI;
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
		public MIDIEvent<NoteOnMessage> NoteOnE;
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

	private readonly List<MIDIEvent<ControllerMessage>> _volEvents;
	private readonly List<MIDIEvent<ControllerMessage>> _panEvents;
	private readonly List<MIDIEvent<PitchBendMessage>> _pitchEvents;
	private readonly List<MIDIEvent<ProgramChangeMessage>> _programEvents;

	/// <summary>Scavenges <paramref name="inTrack"/> for events and splits them into new tracks</summary>
	public TrackData(byte trackIndex, MIDITrackChunk inTrack)
	{
		_trackIndex = trackIndex;
		_inTrack = inTrack;

		_usedPrograms = new HashSet<MIDIProgram>();
		_playingNotes = new List<PlayingNote>();
		_volEvents = new List<MIDIEvent<ControllerMessage>>();
		_panEvents = new List<MIDIEvent<ControllerMessage>>();
		_pitchEvents = new List<MIDIEvent<PitchBendMessage>>();
		_programEvents = new List<MIDIEvent<ProgramChangeMessage>>();

		_trackChannel = GatherTrackInfoFirstPass();

		_newTracks = _usedPrograms.Count == 0 ? null : new NewTrackDict(trackIndex, _trackChannel, _usedPrograms);
	}

	/// <summary>Program changes with no NoteOn are not recorded.</summary>
	private byte GatherTrackInfoFirstPass()
	{
		byte chan = byte.MaxValue;

		for (IMIDIEvent? ev = _inTrack.First; ev is not null; ev = ev.Next)
		{
			switch (ev)
			{
				case MIDIEvent<ProgramChangeMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					_curProgram = e.Msg.Program;
					_programEvents.Add(e);
					break;
				}
				case MIDIEvent<NoteOnMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					if (e.Msg.Velocity != 0)
					{
						_usedPrograms.Add(_curProgram);
					}
					break;
				}
				case MIDIEvent<PitchBendMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					_pitchEvents.Add(e);
					break;
				}
				case MIDIEvent<ControllerMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					switch (e.Msg.Controller)
					{
						case ControllerType.ChannelVolume: _volEvents.Add(e); break;
						case ControllerType.Pan: _panEvents.Add(e); break;
					}
					break;
				}
				default:
				{
					// NoteOff, ChannelPressure, PolyphonicPressure
					if (ev.Msg is IMIDIChannelMessage m)
					{
						CheckChannel(m, ref chan);
					}
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

		for (IMIDIEvent? e = _inTrack.First; e is not null; e = e.Next)
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
	private bool SplitTrack_SpecialMessage(IMIDIEvent ev, NewTrackDict ts, ref NewTrackPattern? curPattern)
	{
		switch (ev)
		{
			case MIDIEvent<ProgramChangeMessage> e:
			{
				_curProgram = e.Msg.Program;
				curPattern = null;
				if (!ts.VoiceIsUsed(_curProgram))
				{
					return false; // Ignore this program change in that case
				}
				break; // Add to all new tracks on this channel
			}
			case MIDIEvent<NoteOnMessage> e:
			{
				if (e.Msg.Velocity == 0)
				{
					SplitTrack_StopPlayingNote(e, ts, e.Msg.Note);
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
			case MIDIEvent<NoteOffMessage> e:
			{
				// TODO: NoteOff velocity? It doesn't always match. NoteOn was 111 and NoteOff was 64
				SplitTrack_StopPlayingNote(e, ts, e.Msg.Note);
				return false; // Only add to correct new track
			}
			case MIDIEvent<MetaMessage> e:
			{
				Console.WriteLine("Track {0}: {1}", _trackIndex, e);

				if (e.Msg.Type == MetaMessageType.TrackName)
				{
					return false; // Ignore these
				}
				break; // Add to all new tracks
			}
		}

		return true;
	}
	private void SplitTrack_StopPlayingNote(IMIDIEvent e, NewTrackDict ts, MIDINote n)
	{
		// Try to catch the oldest first
		for (int i = 0; i < _playingNotes.Count; i++)
		{
			PlayingNote p = _playingNotes[i];
			NoteOnMessage noteOn = p.NoteOnE.Msg;
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
