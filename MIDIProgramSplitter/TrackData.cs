using Kermalis.MIDI;
using MIDIProgramSplitter.FLP;
using System;
using System.Collections.Generic;
using System.IO;

namespace MIDIProgramSplitter;

// Assumes a format1 track
internal sealed class TrackData
{
	private readonly byte _trackIndex;
	private readonly MIDITrackChunk _inTrack;
	private readonly byte _trackChannel;

	/// <summary>Each HashSet keeps track of the programs that have NoteOn</summary>
	private readonly HashSet<MIDIProgram> _usedPrograms;
	/// <summary>Used while iterating the events</summary>
	private MIDIProgram _curProgram;
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

		_volEvents = new List<MIDIEvent>();
		_panEvents = new List<MIDIEvent>();
		_pitchEvents = new List<MIDIEvent>();
		_programEvents = new List<MIDIEvent>();

		_trackChannel = RecordAllUsedProgramsInTrack();

		_newTracks = _usedPrograms.Count == 0
			? null
			: new NewTrackDict(trackIndex, _trackChannel, _usedPrograms, _programEvents, (uint)_inTrack.NumTicks);
	}

	/// <summary>Searches for all program changes in <see cref="_inTrack"/>. <see cref="_usedPrograms"/>'s elements correspond to each MIDI channel. Program changes with no NoteOn are not recorded.</summary>
	private byte RecordAllUsedProgramsInTrack()
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
		_curProgram = 0;
		var playingNotes = new List<(MIDIProgram, NoteOnMessage)>();

		for (MIDIEvent? e = _inTrack.First; e is not null; e = e.Next)
		{
			if (SplitTrack_SpecialMessage(e, playingNotes))
			{
				continue;
			}

			// Add to all new tracks
			_newTracks?.InsertEventIntoAllNewTracks(e);
		}
	}
	/// <summary>Returns true if this event should not be added to the new tracks</summary>
	private bool SplitTrack_SpecialMessage(MIDIEvent e, List<(MIDIProgram, NoteOnMessage)> playingNotes)
	{
		MIDIMessage msg = e.Message;
		switch (msg)
		{
			case ProgramChangeMessage m:
			{
				_curProgram = m.Program;
				NewTrackDict? ts = _newTracks; // May be null if there are no notes and no other program changes

				if (ts is null || !ts.VoiceIsUsed(m.Program))
				{
					return true; // Ignore this program change in that case
				}
				break; // Add to all new tracks on this channel
			}
			case NoteOnMessage m:
			{
				NewTrackDict? ts = _newTracks;
				if (ts is null)
				{
					throw new InvalidDataException("NoteOn track issue...");
				}

				if (m.Velocity == 0)
				{
					SplitTrack_StopPlayingNote(e, playingNotes, ts, m.Note);
				}
				else
				{
					playingNotes.Add((_curProgram, m));

					ts.InsertEventIntoNewTrack(e, _curProgram);
				}
				return true; // Only add to correct new track
			}
			case NoteOffMessage m:
			{
				NewTrackDict? ts = _newTracks;
				if (ts is null)
				{
					throw new InvalidDataException("There was a NoteOff message in a track with no NoteOn...");
				}

				// TODO: NoteOff velocity? It doesn't always match. NoteOn was 111 and NoteOff was 64
				SplitTrack_StopPlayingNote(e, playingNotes, ts, m.Note);
				return true; // Only add to correct new track
			}
			case MetaMessage m:
			{
				Console.WriteLine("Track {0}: {1}", _trackIndex, m);

				if (m.Type == MetaMessageType.TrackName)
				{
					return true; // Ignore these
				}
				break; // Add to all new tracks
			}
		}

		return false;
	}
	private void SplitTrack_StopPlayingNote(MIDIEvent e, List<(MIDIProgram, NoteOnMessage)> playingNotes, NewTrackDict ts, MIDINote n)
	{
		// Try to catch the oldest first
		for (int i = 0; i < playingNotes.Count; i++)
		{
			(MIDIProgram playingP, NoteOnMessage noteOn) = playingNotes[i];
			if (noteOn.Note == n) // Check channel also if not Format1
			{
				playingNotes.RemoveAt(i);
				ts.InsertEventIntoNewTrack(e, playingP);
				return;
			}
		}
		throw new InvalidDataException($"Track {_trackIndex}: NoteOff without a prior NoteOn...");
	}

	public void AddNewTracks(MIDIFile outMIDI)
	{
		if (_newTracks is null)
		{
			return;
		}

		foreach (KeyValuePair<MIDIProgram, NewTrack> kvp in _newTracks.Dict)
		{
			outMIDI.AddChunk(kvp.Value.Track);
		}
	}
	public void FLP_HandleTrack(FLProjectWriter w, uint maxTicks)
	{
		if (_newTracks is null)
		{
			return; // TODO: Don't ignore automation
		}

		Dictionary<MIDIProgram, NewTrack> dict = _newTracks.Dict;
		List<FLChannel> ourChans = FLP_CreateChannels(w, dict);
		FLP_CreatePatterns(w, ourChans, dict, maxTicks);
		FLP_CreateAutomations(w, ourChans, maxTicks);
	}
	private List<FLChannel> FLP_CreateChannels(FLProjectWriter w, Dictionary<MIDIProgram, NewTrack> dict)
	{
		var ourChans = new List<FLChannel>();
		foreach (NewTrack newT in dict.Values)
		{
			var c = new FLChannel(newT.Name, _trackChannel, newT.Program);
			w.Channels.Add(c);
			ourChans.Add(c);
		}
		return ourChans;
	}
	private void FLP_CreatePatterns(FLProjectWriter w, List<FLChannel> ourChans, Dictionary<MIDIProgram, NewTrack> dict, uint maxTicks)
	{
		int ourChanID = 0;
		foreach (NewTrack newT in dict.Values)
		{
			// Randomize pattern colors for fun
			var p = new FLPattern
			{
				Color = new FLColor3((uint)Random.Shared.Next(0x1_000_000)),
			};
			w.Patterns.Add(p);

			FLChannel ourChan = ourChans[ourChanID++];
			ushort chanID = (ushort)w.Channels.IndexOf(ourChan);

			FLP_AddNotes(p, newT, chanID);
			FLPlaylistTrack pTrack = w.PlaylistTracks[_trackIndex];
			pTrack.Name = "T" + (_trackIndex + 1) + 'C' + (_trackChannel + 1);

			w.AddToPlaylist(p, 0, maxTicks, pTrack);

			// TODO: Chop up the new tracks into multiple patterns
			//(uint startTick, uint durationTicks) = newT.JankyStartEndList.Single(((uint, uint) tup) => tup.Item1 <= p.Notes[0].AbsoluteTick && p.no);

			//w.AddToPlaylist(p, startTick, durationTicks, pTrack);
		}
	}
	private void FLP_AddNotes(FLPattern p, NewTrack newT, ushort chanID)
	{
		var playingNotes = new List<MIDIEvent>();

		for (MIDIEvent? e = newT.Track.First; e is not null; e = e.Next)
		{
			switch (e.Message)
			{
				case NoteOnMessage m:
				{
					if (m.Velocity == 0)
					{
						FLP_StopPlayingNote(p, chanID, e, m.Note, playingNotes);
					}
					else
					{
						playingNotes.Add(e);
					}
					break;
				}
				case NoteOffMessage m:
				{
					// TODO: NoteOff velocity? It doesn't always match. NoteOn was 111 and NoteOff was 64
					FLP_StopPlayingNote(p, chanID, e, m.Note, playingNotes);
					break;
				}
			}
		}

		if (playingNotes.Count != 0)
		{
			throw new InvalidDataException($"Track {_trackIndex}: NoteOff and NoteOn count mismatch...");
		}
	}
	private void FLP_StopPlayingNote(FLPattern p, ushort chanID, MIDIEvent e, MIDINote n, List<MIDIEvent> playingNotes)
	{
		// Try to catch the oldest first
		for (int i = 0; i < playingNotes.Count; i++)
		{
			MIDIEvent playingE = playingNotes[i];
			var noteOn = (NoteOnMessage)playingE.Message;
			if (noteOn.Note == n) // Check channel also if not Format1
			{
				playingNotes.RemoveAt(i);
				p.Notes.Add(new FLPatternNote
				{
					Channel = chanID,
					AbsoluteTick = (uint)playingE.Ticks,
					DurationTicks = (uint)(e.Ticks - playingE.Ticks),
					Key = (byte)noteOn.Note,
					Velocity = noteOn.Velocity,
				});
				return;
			}
		}
		throw new InvalidDataException($"Track {_trackIndex}: NoteOff without a prior NoteOn...");
	}
	private void FLP_CreateAutomations(FLProjectWriter w, List<FLChannel> ourChans, uint maxTicks)
	{
		bool groupWithAbove = false;

		// TODO: If there are 0 or 1 events, don't create automation pls
		if (_volEvents.Count != 0)
		{
			FLAutomation a = CreateAuto("Volume", FLAutomation.MyType.Volume, ourChans);
			foreach (MIDIEvent e in _volEvents)
			{
				byte v = ((ControllerMessage)e.Message).Value;
				a.AddPoint((uint)e.Ticks, v / 127d);
			}
			AddAuto(w, a, maxTicks, 1d, ref groupWithAbove); // I believe MIDI defaults to max channel volume
		}
		if (_panEvents.Count != 0)
		{
			FLAutomation a = CreateAuto("Panpot", FLAutomation.MyType.Panpot, ourChans);
			foreach (MIDIEvent e in _panEvents)
			{
				byte v = ((ControllerMessage)e.Message).Value;
				// 0 => 100% left, 64 => center, 127 => 100% right
				double dv; // Split the operation to ensure 0.5 is centered
				if (v <= 64)
				{
					dv = Utils.LerpUnclamped(0, 64, 0, 0.5, v);
				}
				else
				{
					dv = Utils.LerpUnclamped(64, 127, 0.5, 1, v);
				}
				a.AddPoint((uint)e.Ticks, dv);
			}
			AddAuto(w, a, maxTicks, 0.5d, ref groupWithAbove); // Centered
		}
		if (_pitchEvents.Count != 0)
		{
			FLAutomation a = CreateAuto("Pitch", FLAutomation.MyType.Pitch, ourChans);
			foreach (MIDIEvent e in _pitchEvents)
			{
				//int v = ((PitchBendMessage)e.Message).GetPitchAsInt();

				// TODO: Get correct cents values
				// This is theoretically correct:
				//double dv = Utils.LerpUnclamped(-8192, 8191, 0, 1, v); // 0 => -4800 cents, 0.5 => +0 cents, 1.0 => 4800 cents
				//double dv = Utils.LerpUnclamped(-256*256, (256*256)-1, 0, 1, v);

				// This method gets close. Target is -4 but it gives -3. Probably -4 is a rounding error in GBA and -3 is correct
				double range = 127d / 9600;
				double dv = Utils.LerpUnclamped(-64, 63, 0.5 - range, 0.5 + range, ((PitchBendMessage)e.Message).MSB - 64);

				a.AddPoint((uint)e.Ticks, dv);
			}
			AddAuto(w, a, maxTicks, 0.5d, ref groupWithAbove); // +0 cents
		}
		if (_programEvents.Count != 0)
		{
			FLAutomation a = CreateAuto("Instrument", FLAutomation.MyType.MIDIProgram, ourChans);
			foreach (MIDIEvent e in _programEvents)
			{
				byte v = (byte)(((ProgramChangeMessage)e.Message).Program + 1);
				a.AddPoint((uint)e.Ticks, v / 128d);
			}
			AddAuto(w, a, maxTicks, 1 / 128d, ref groupWithAbove); // Program 0
		}
	}
	private FLAutomation CreateAuto(string type, FLAutomation.MyType flType, List<FLChannel> ourChans)
	{
		return new FLAutomation($"Track {_trackIndex + 1} {type}", flType, ourChans);
	}
	private static void AddAuto(FLProjectWriter w, FLAutomation a, uint maxTicks, double defaultVal, ref bool groupWithAbove)
	{
		a.PadPoints(maxTicks, defaultVal);
		w.Automations.Add(a);

		FLPlaylistTrack track = w.PlaylistTracks[w.PlaylistItems.Count + 16]; // TODO: Intelligence
		track.GroupWithAbove = groupWithAbove;
		groupWithAbove = true;
		w.AddToPlaylist(a, 0, maxTicks, track);
	}
}
