using Kermalis.MIDI;
using System;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

// MIDI Format 0 would have multiple channels per track. 1 and 2 do not
internal sealed class TrackData
{
	private readonly byte _trackIndex;
	private readonly MIDITrackChunk _inTrack;

	/// <summary>Per channel. Each HashSet keeps track of the programs that have NoteOn</summary>
	private readonly HashSet<MIDIProgram>[] _usedPrograms;
	/// <summary>Used while iterating the events</summary>
	private readonly MIDIProgram[] _curPrograms;
	private readonly TrackSplit?[] _newTracks;

	/// <summary>Scavenges <paramref name="inTrack"/> for events and splits them into new tracks for <paramref name="outMIDI"/></summary>
	public TrackData(byte trackIndex, MIDITrackChunk inTrack, OutMIDI outMIDI)
	{
		_trackIndex = trackIndex;
		_inTrack = inTrack;

		_usedPrograms = new HashSet<MIDIProgram>[16];
		_curPrograms = new MIDIProgram[16];

		for (byte c = 0; c < 16; c++)
		{
			_usedPrograms[c] = new HashSet<MIDIProgram>();
		}

		RecordAllUsedProgramsInTrack();

		_newTracks = new TrackSplit?[16];
		for (byte c = 0; c < 16; c++)
		{
			HashSet<MIDIProgram> used = _usedPrograms[c];
			_newTracks[c] = used.Count == 0 ? null : new TrackSplit(trackIndex, c, used, outMIDI);
		}
	}

	/// <summary>Searches for all program changes in <see cref="_inTrack"/>. <see cref="_usedPrograms"/>'s elements correspond to each MIDI channel. Program changes with no NoteOn are not recorded.</summary>
	private void RecordAllUsedProgramsInTrack()
	{
		for (MIDIEvent? e = _inTrack.First; e is not null; e = e.Next)
		{
			switch (e.Message)
			{
				case ProgramChangeMessage m:
				{
					byte chan = m.Channel;
					_curPrograms[chan] = m.Program;
					break;
				}
				case NoteOnMessage m:
				{
					byte chan = m.Channel;
					_usedPrograms[chan].Add(_curPrograms[chan]);
					break;
				}
			}
		}
	}

	/// <summary>Iterates all messages in <see cref="_inTrack"/> and copies/splits them to the new tracks</summary>
	public void SplitTrack()
	{
		Array.Clear(_curPrograms);

		for (MIDIEvent? e = _inTrack.First; e is not null; e = e.Next)
		{
			if (SplitTrack_SpecialMessage(e))
			{
				continue;
			}

			if (e.Message is IMIDIChannelMessage cm)
			{
				// Add to all new tracks on this channel
				_newTracks[cm.Channel]?.InsertEventIntoAllNewTracks(e);
			}
			else // Probably a global message like EndOfTrack
			{
				// Add to all new tracks
				foreach (TrackSplit? ts in _newTracks)
				{
					ts?.InsertEventIntoAllNewTracks(e);
				}
			}
		}
	}
	/// <summary>Returns true if this event should not be added to the new tracks</summary>
	private bool SplitTrack_SpecialMessage(MIDIEvent e)
	{
		MIDIMessage msg = e.Message;
		switch (msg)
		{
			case ProgramChangeMessage m:
			{
				byte chan = m.Channel;
				_curPrograms[chan] = m.Program;
				TrackSplit? ts = _newTracks[chan]; // May be null if there are no notes and no other program changes

				if (ts is null || !ts.VoiceIsUsed(m.Program))
				{
					return true; // Ignore this program change in that case
				}
				break; // Add to all new tracks on this channel
			}
			case NoteOnMessage:
			case NoteOffMessage:
			{
				var m = (IMIDIChannelMessage)msg;
				byte chan = m.Channel;
				MIDIProgram curVoice = _curPrograms[chan];
				TrackSplit ts = _newTracks[chan]!; // Not null if there are notes

				ts.InsertEventIntoNewTrack(e, curVoice);
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
}
