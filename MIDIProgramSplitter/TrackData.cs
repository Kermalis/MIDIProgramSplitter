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
		public IMIDIEvent<NoteOnMessage> NoteOnE;
	}

	private const byte DEFAULT_MIDI_PAN = 64; // Center
	private const int DEFAULT_MIDI_PITCH = 0;
	private const MIDIProgram DEFAULT_MIDI_PROGRAM = 0;

	private readonly byte _trackIndex;
	private readonly MIDITrackChunk _inTrack;
	private readonly byte _trackChannel;

	/// <summary>Keeps track of the programs that have NoteOn</summary>
	private readonly HashSet<MIDIProgram> _usedPrograms;
	/// <summary>Used while iterating the events</summary>
	private MIDIProgram _curProgram;
	private readonly List<PlayingNote> _playingNotes;
	/// <summary>The new tracks created</summary>
	private readonly NewTrackDict? _newTracks; // Allow null I guess since you could be copying automations for later with no notes

	private readonly List<IMIDIEvent<ControllerMessage>> _volEvents;
	private readonly List<IMIDIEvent<ControllerMessage>> _panEvents;
	private readonly List<IMIDIEvent<PitchBendMessage>> _pitchEvents;
	private readonly List<IMIDIEvent<ProgramChangeMessage>> _programEvents;

	private readonly List<IMIDIEvent<ControllerMessage>> _volEventsOptimized;
	private readonly List<IMIDIEvent<ControllerMessage>> _panEventsOptimized;
	private readonly List<IMIDIEvent<PitchBendMessage>> _pitchEventsOptimized;
	private readonly List<IMIDIEvent<ProgramChangeMessage>> _programEventsOptimized;

	/// <summary>Scavenges <paramref name="inTrack"/> for events and splits them into new tracks</summary>
	public TrackData(byte trackIndex, MIDITrackChunk inTrack, byte defaultMIDIVol, Splitter split)
	{
		_trackIndex = trackIndex;
		_inTrack = inTrack;

		_usedPrograms = new HashSet<MIDIProgram>();
		_playingNotes = new List<PlayingNote>();
		_volEvents = new List<IMIDIEvent<ControllerMessage>>();
		_panEvents = new List<IMIDIEvent<ControllerMessage>>();
		_pitchEvents = new List<IMIDIEvent<PitchBendMessage>>();
		_programEvents = new List<IMIDIEvent<ProgramChangeMessage>>();
		_volEventsOptimized = new List<IMIDIEvent<ControllerMessage>>();
		_panEventsOptimized = new List<IMIDIEvent<ControllerMessage>>();
		_pitchEventsOptimized = new List<IMIDIEvent<PitchBendMessage>>();
		_programEventsOptimized = new List<IMIDIEvent<ProgramChangeMessage>>();

		_trackChannel = GatherTrackInfoFirstPass(defaultMIDIVol, split);

		_newTracks = _usedPrograms.Count == 0 ? null : new NewTrackDict(trackIndex, _trackChannel, _usedPrograms);
	}

	/// <summary>Program changes with no NoteOn are not recorded.</summary>
	private byte GatherTrackInfoFirstPass(byte defaultMIDIVol, Splitter split)
	{
		Console.WriteLine();
		split.Log(string.Format("Beginning first pass on Track {0}...", _trackIndex));

		byte chan = byte.MaxValue;

		// Don't include default values if they're first
		_curProgram = DEFAULT_MIDI_PROGRAM;
		MIDIProgram curOptimizedProgram = DEFAULT_MIDI_PROGRAM;
		byte curVolume = defaultMIDIVol;
		byte curPan = DEFAULT_MIDI_PAN;
		int curPitch = DEFAULT_MIDI_PITCH;

		IMIDIEvent<ProgramChangeMessage>? pendingProgramChange = null;

		for (IMIDIEvent? ev = _inTrack.First; ev is not null; ev = ev.Next)
		{
			switch (ev)
			{
				case IMIDIEvent<ProgramChangeMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					_curProgram = e.Msg.Program;

					_programEvents.Add(e);
					// Don't add these with optimization unless they are used.
					// Example is BrassSection towards the end of HGSS rival
					pendingProgramChange = e;
					break;
				}
				case IMIDIEvent<NoteOnMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					// If it's not a NoteOff, add it to the lists
					if (e.Msg.Velocity != 0)
					{
						// Always count this program as used if there is a NoteOn
						// It's common to have no program change before a piano note
						_usedPrograms.Add(_curProgram); // Adds if it's not already present in the hashset

						// Now deal with optimizing the program changes
						if (pendingProgramChange is not null)
						{
							if (curOptimizedProgram != _curProgram)
							{
								_programEventsOptimized.Add(pendingProgramChange);
							}
							curOptimizedProgram = _curProgram;
							pendingProgramChange = null;
						}
					}
					break;
				}
				case IMIDIEvent<PitchBendMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					int newPitch = e.Msg.GetPitchAsInt();

					_pitchEvents.Add(e);
					if (curPitch != newPitch)
					{
						_pitchEventsOptimized.Add(e);
					}
					curPitch = newPitch;
					break;
				}
				case IMIDIEvent<ControllerMessage> e:
				{
					CheckChannel(e.Msg, ref chan);
					byte newVal = e.Msg.Value;

					switch (e.Msg.Controller)
					{
						case ControllerType.ChannelVolume:
						{
							_volEvents.Add(e);
							if (curVolume != newVal)
							{
								_volEventsOptimized.Add(e);
							}
							curVolume = newVal;
							break;
						}
						case ControllerType.Pan:
						{
							_panEvents.Add(e);
							if (curPan != newVal)
							{
								_panEventsOptimized.Add(e);
							}
							curPan = newVal;
							break;
						}
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

			// Check for a program change after a NoteOn in the same tick
			if (ev.Msg is ProgramChangeMessage)
			{
				CheckProgramChangeWarning(ev, split);
			}
		}

		if (chan == byte.MaxValue)
		{
			// TODO: Allow, but skip track
			throw new InvalidDataException($"Track {_trackIndex} had no MIDI channel events...");
		}
		split.Log(string.Format("Track {0} detected with channel {1}!", _trackIndex, chan));
		return chan;
	}
	private static void CheckProgramChangeWarning(IMIDIEvent ev, Splitter split)
	{
		for (IMIDIEvent? prev = ev.Prev; prev is not null; prev = prev.Prev)
		{
			if (prev.Ticks != ev.Ticks)
			{
				return;
			}
			if (prev.Msg is NoteOnMessage n)
			{
				split.Log(string.Format("Warning: Two events at the same tick: @{2} = ({0}) & ({1})", n, ev.Msg, ev.Ticks));
			}
		}
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
	public void SplitTrack(Splitter split)
	{
		if (_newTracks is null)
		{
			split.Log(string.Format("Skipping track {0} because it has no notes...", _trackIndex));
			return;
		}

		// TODO: Option to only use optimized events. Would need to check if the event is in the optimized list

		NewTrackDict ts = _newTracks;
		_curProgram = DEFAULT_MIDI_PROGRAM;
		NewTrackPattern? curPattern = null;

		for (IMIDIEvent? e = _inTrack.First; e is not null; e = e.Next)
		{
			if (SplitTrack_SpecialMessage(e, ts, split, ref curPattern))
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
	private bool SplitTrack_SpecialMessage(IMIDIEvent ev, NewTrackDict ts, Splitter split, ref NewTrackPattern? curPattern)
	{
		switch (ev)
		{
			case IMIDIEvent<ProgramChangeMessage> e:
			{
				_curProgram = e.Msg.Program;
				if (_programEventsOptimized.Contains(e))
				{
					curPattern = null;
				}
				if (!ts.VoiceIsUsed(_curProgram))
				{
					return false; // Ignore this program change in that case
				}
				break; // Add to all new tracks on this channel
			}
			case IMIDIEvent<NoteOnMessage> e:
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
						NoteOnE = e,
					});

					ts.InsertEventIntoNewTrack(e, _curProgram);
				}
				return false; // Only add to correct new track
			}
			case IMIDIEvent<NoteOffMessage> e:
			{
				// TODO: NoteOff velocity? It doesn't always match. NoteOn was 111 and NoteOff was 64
				SplitTrack_StopPlayingNote(e, ts, e.Msg.Note);
				return false; // Only add to correct new track
			}
			case IMIDIEvent<MetaMessage> e:
			{
				split.Log(string.Format("Track {0}: {1}", _trackIndex, e));

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
