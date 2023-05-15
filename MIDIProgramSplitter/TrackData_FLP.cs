using FLP;
using Kermalis.MIDI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MIDIProgramSplitter;

partial class TrackData
{
	public void FLP_AddNewTracks(FLPSaver saver)
	{
		if (_newTracks is null)
		{
			return; // TODO: Don't ignore automation?
		}

		Dictionary<MIDIProgram, NewTrack> dict = _newTracks.Dict;
		string shortName = string.Format("T{0:D2}", _trackIndex);
		string longName = shortName + string.Format("C{0:D2}", _trackChannel + 1);

		FLChannelFilter f = saver.FLP.CreateChannelFilter(longName);
		List<FLChannel> ourChans = FLP_CreateChannels(saver, f, dict);
		FLP_CreatePatterns(saver, shortName, ourChans, dict);
		FLP_CreateAutomations(saver, f, ourChans);
	}
	private List<FLChannel> FLP_CreateChannels(FLPSaver saver, FLChannelFilter filter, Dictionary<MIDIProgram, NewTrack> dict)
	{
		FLPlaylistTrack instTrack = GetInstTrack(saver.FLP.Arrangements[0]);
		byte midiChan = _trackChannel; // TODO: Option to make every MIDI track have a unique channel
		byte midiBank = (byte)(_trackIndex - 1); // Meta track doesn't have one

		var ourChans = new List<FLChannel>();
		foreach (NewTrack newT in dict.Values)
		{
			FLChannel c = saver.FLP.CreateChannel(newT.Name, midiChan, midiBank, newT.Program, filter);
			c.Color = saver.Options.GetMIDIOutColor(newT.Program, instTrack);
			c.PitchBendRange = saver.Options.PitchBendRange;
			ourChans.Add(c);
		}
		return ourChans;
	}
	private void FLP_CreatePatterns(FLPSaver saver, string name, List<FLChannel> ourChans, Dictionary<MIDIProgram, NewTrack> dict)
	{
		FLPlaylistTrack instTrack = GetInstTrack(saver.FLP.Arrangements[0]);
		instTrack.Name = name;

		var createdPats = new List<(NewTrackPattern, FLPattern)>();

		int ourChanID = 0;
		int ourPatID = 1;
		foreach (NewTrack newT in dict.Values)
		{
			FLChannel ourChan = ourChans[ourChanID++];

			// TODO: Why is #3 before #2 sometimes etc. Is it the dict order?
			// Most likely the dict order. it explains why the patterns weren't in order of absoluteTick
			foreach (NewTrackPattern newP in newT.Patterns)
			{
				// First check if an identical pattern exists
				if (FLP_CheckForDuplicatePattern(createdPats, newP, out FLPattern? flPat))
				{
					newP.AddToFLP_Duplicate(saver, flPat, instTrack);
				}
				else
				{
					string pName = string.Format("{0} #{1}", name, ourPatID++);
					if (saver.Options.AppendInstrumentNamesToPatterns)
					{
						pName += " - " + newT.Program;
					}
					flPat = newP.AddToFLP(saver, ourChan, instTrack, newT.Program, pName);
					createdPats.Add((newP, flPat));
				}
			}
		}
	}
	private static bool FLP_CheckForDuplicatePattern(List<(NewTrackPattern, FLPattern)> createdPats, NewTrackPattern newP,
		[NotNullWhen(true)] out FLPattern? flPat)
	{
		foreach ((NewTrackPattern, FLPattern) tup in createdPats)
		{
			if (tup.Item1.SequenceEqual(newP))
			{
				flPat = tup.Item2;
				return true;
			}
		}
		flPat = null;
		return false;
	}

	private void FLP_CreateAutomations(FLPSaver saver, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		if (saver.Options.AutomationGrouping != FLPSaveOptions.AutomationGroupMode.GroupAll)
		{
			saver.CurGroupWithAbove = false;
		}

		// TODO: Option to not use the optimized lists
		HandleVolumeEvents(saver, filter, ourChans);
		HandlePanpotEvents(saver, filter, ourChans);
		HandlePitchEvents(saver, filter, ourChans);
		HandleProgramEvents(saver, filter, ourChans);
	}

	private FLPlaylistTrack GetInstTrack(FLArrangement arrang)
	{
		return arrang.PlaylistTracks[_trackIndex - 1]; // Meta track won't have one
	}

	private void HandleVolumeEvents(FLPSaver saver, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		// TODO: This relies on the DefaultMIDIVol being the same in the flp options as it was when creating this trackdata
		byte midiVol;
		if (_volEventsOptimized.Count == 0)
		{
			midiVol = saver.Options.DefaultMIDIVolume;
		}
		else if (_volEventsOptimized.Count == 1 && _volEventsOptimized[0].Ticks == 0) // TODO: These with just 1 must be a 1 BEFORE any notes. AKA keep the default values in this class
		{
			midiVol = _volEventsOptimized[0].Msg.Value;
		}
		else // For >= 2 events, create an automation clip
		{
			FLAutomation a = CreateAuto(saver, "Volume", FLAutomation.MyType.Volume, filter, ourChans);
			foreach (IMIDIEvent<ControllerMessage> e in _volEventsOptimized)
			{
				a.AddPoint((uint)e.Ticks, VolumeToAutomation(e.Msg.Value));
			}
			saver.AddMIDITrackAuto(a, VolumeToAutomation(saver.Options.DefaultMIDIVolume));
			return;
		}

		// For 0 or 1 event, just set the knob
		uint volKnob = VolumeToKnob(midiVol);
		foreach (FLChannel c in ourChans)
		{
			c.VolKnob = volKnob;
		}
	}
	private static uint VolumeToKnob(byte vol)
	{
		return (uint)FLUtils.LerpUnclamped(0f, 127, FLBasicChannelParams.KNOB_MIN, FLBasicChannelParams.KNOB_MAX, vol);
	}
	private static double VolumeToAutomation(byte vol)
	{
		return vol / 127d;
	}

	private void HandlePanpotEvents(FLPSaver saver, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		byte midiPan;
		if (_panEventsOptimized.Count == 0)
		{
			midiPan = DEFAULT_MIDI_PAN;
		}
		else if (_panEventsOptimized.Count == 1 && _panEventsOptimized[0].Ticks == 0)
		{
			midiPan = _panEventsOptimized[0].Msg.Value;
		}
		else // For >= 2 events, create an automation clip
		{
			FLAutomation a = CreateAuto(saver, "Panpot", FLAutomation.MyType.Panpot, filter, ourChans);
			foreach (IMIDIEvent<ControllerMessage> e in _panEventsOptimized)
			{
				a.AddPoint((uint)e.Ticks, PanpotToAutomation(e.Msg.Value));
			}
			saver.AddMIDITrackAuto(a, PanpotToAutomation(DEFAULT_MIDI_PAN));
			return;
		}

		// For 0 or 1 event, just set the knob
		uint panKnob = PanpotToKnob(midiPan);
		foreach (FLChannel c in ourChans)
		{
			c.PanKnob = panKnob;
		}
	}
	// 0 => 100% left, 64 => center, 127 => 100% right
	// Split the operation to ensure the center is correct
	private static uint PanpotToKnob(byte pan)
	{
		if (pan <= 64)
		{
			return (uint)FLUtils.LerpUnclamped(0, 64f, FLBasicChannelParams.KNOB_MIN, FLBasicChannelParams.KNOB_HALF, pan);
		}
		return (uint)FLUtils.LerpUnclamped(64f, 127, FLBasicChannelParams.KNOB_HALF, FLBasicChannelParams.KNOB_MAX, pan);
	}
	private static double PanpotToAutomation(byte pan)
	{
		if (pan <= 64)
		{
			return FLUtils.LerpUnclamped(0, 64, 0, 0.5, pan);
		}
		return FLUtils.LerpUnclamped(64, 127, 0.5, 1, pan);
	}

	private void HandlePitchEvents(FLPSaver saver, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		double unitsPerCent = 8_192d / (saver.Options.PitchBendRange * 100);
		int midiPitch;
		if (_pitchEventsOptimized.Count == 0)
		{
			midiPitch = DEFAULT_MIDI_PITCH;
		}
		else if (_pitchEventsOptimized.Count == 1 && _pitchEventsOptimized[0].Ticks == 0)
		{
			midiPitch = _pitchEventsOptimized[0].Msg.GetPitchAsInt();
		}
		else // For >= 2 events, create an automation clip
		{
			FLAutomation a = CreateAuto(saver, "Pitch", FLAutomation.MyType.Pitch, filter, ourChans);
			a.PitchBendOrTimeRange = saver.Options.PitchBendRange;
			foreach (IMIDIEvent<PitchBendMessage> e in _pitchEventsOptimized)
			{
				a.AddPoint((uint)e.Ticks, PitchToAutomation(e.Msg.GetPitchAsInt(), unitsPerCent));
			}
			saver.AddMIDITrackAuto(a, PitchToAutomation(DEFAULT_MIDI_PITCH, unitsPerCent));
			return;
		}

		// For 0 or 1 event, just set the knob
		int pitchKnob = PitchToKnob(midiPitch, unitsPerCent);
		foreach (FLChannel c in ourChans)
		{
			c.PitchKnob = pitchKnob;
		}
	}
	private static int PitchToKnob(int midiUnits, double unitsPerCent)
	{
		return (int)(midiUnits / unitsPerCent);
	}
	private static double PitchToAutomation(int midiUnits, double unitsPerCent)
	{
		// midiUnits is [-8192, 8191]
		// 0 => -4800 cents, 0.5 => +0 cents, 1.0 => 4800 cents
		return FLUtils.LerpUnclamped(-4800, 4800, 0, 1, midiUnits / unitsPerCent);
	}

	private void HandleProgramEvents(FLPSaver saver, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		// TODO: Instrument automations may be weird if they're on the same tick as a note. No real way to control the order in which it happens.
		// For that case, we need to split every MIDIOut into a unique chan/bank and not do instrument automations
		// We need to warn the user that there are events and notes on the same tick so they can take action if required
		// So make this an option. Don't need instrument autos if every track goes to a separate fruityLSD, and each split instrument is on a separate channel. Cannot have more than 16 unique instruments per FruityLSD
		bool outputInstrumentAutos = true;

		MIDIProgram midiProgram;
		if (_programEventsOptimized.Count == 0)
		{
			midiProgram = DEFAULT_MIDI_PROGRAM; // This count can be 0 for optimized if the program is the default one and it is used
		}
		else if (_programEventsOptimized.Count == 1 && _programEventsOptimized[0].Ticks == 0)
		{
			midiProgram = _programEventsOptimized[0].Msg.Program;
		}
		else // For >= 2 events, create an automation clip
		{
			if (outputInstrumentAutos)
			{
				FLAutomation a = CreateAuto(saver, "Instrument", FLAutomation.MyType.MIDIProgram, filter, ourChans);
				foreach (IMIDIEvent<ProgramChangeMessage> e in _programEventsOptimized)
				{
					a.AddPoint((uint)e.Ticks, ProgramToAutomation(e.Msg.Program));
				}
				saver.AddMIDITrackAuto(a, ProgramToAutomation(DEFAULT_MIDI_PROGRAM));
			}
			return;
		}

		// For 0 or 1 event, just set the program. We would have only 1 channel here
		Debug.Assert(ourChans.Count == 1);
		ourChans[0].MIDIProgram = midiProgram;
	}
	private static double ProgramToAutomation(MIDIProgram program)
	{
		return (double)(program + 1) / 128d;
	}

	private FLAutomation CreateAuto(FLPSaver saver, string type, FLAutomation.MyType flType, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		FLArrangement arrang = saver.FLP.Arrangements[0];

		FLPlaylistTrack instTrack = GetInstTrack(arrang);
		FLPlaylistTrack autoTrack = arrang.PlaylistTracks[saver.AutomationTrackIndex];
		autoTrack.Color = saver.Options.GetAutomationTrackColor(instTrack);

		FLAutomation a = saver.FLP.CreateAutomation($"Track {_trackIndex} {type}", flType, ourChans, filter);
		a.Color = saver.Options.GetAutomationColor(flType, autoTrack);
		return a;
	}
}
