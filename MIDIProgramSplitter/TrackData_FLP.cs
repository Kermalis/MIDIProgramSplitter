﻿using FLP;
using Kermalis.MIDI;
using System.Collections.Generic;

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
		string name = string.Format("T{0:D2}C{1:D2}", _trackIndex + 1, _trackChannel + 1);

		FLChannelFilter f = saver.FLP.CreateChannelFilter(name);
		List<FLChannel> ourChans = FLP_CreateChannels(saver, f, dict);
		FLP_CreatePatterns(saver, name, ourChans, dict);
		FLP_CreateAutomations(saver, f, ourChans);
	}
	private List<FLChannel> FLP_CreateChannels(FLPSaver saver, FLChannelFilter filter, Dictionary<MIDIProgram, NewTrack> dict)
	{
		byte midiChan = _trackChannel; // TODO: Option to make every MIDI track have a unique channel
		byte midiBank = (byte)(_trackIndex - 1); // Meta track doesn't have one

		var ourChans = new List<FLChannel>();
		foreach (NewTrack newT in dict.Values)
		{
			ourChans.Add(saver.FLP.CreateChannel(newT.Name, midiChan, midiBank, newT.Program, filter));
		}
		return ourChans;
	}
	private void FLP_CreatePatterns(FLPSaver saver, string name, List<FLChannel> ourChans, Dictionary<MIDIProgram, NewTrack> dict)
	{
		FLArrangement arrang = saver.FLP.Arrangements[0];
		FLPlaylistTrack pTrack = arrang.PlaylistTracks[_trackIndex - 1]; // Meta track won't have one
		pTrack.Name = name;

		int ourChanID = 0;
		int ourPatID = 1;
		foreach (NewTrack newT in dict.Values)
		{
			FLChannel ourChan = ourChans[ourChanID++];

			// TODO: Why is #3 before #2 sometimes etc. Is it the dict order?
			// Most likely the dict order. it explains why the patterns weren't in order of absoluteTick
			foreach (NewTrackPattern newP in newT.Patterns)
			{
				string pName = string.Format("{0} #{1} - {2}", name, ourPatID++, newT.Program);
				newP.AddToFLP(saver, ourChan, pTrack, newT.Program, pName);
			}
		}
	}
	private void FLP_CreateAutomations(FLPSaver saver, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		bool outputInstrumentAutos = true; // TODO: Make it an option. Don't need instrument autos if every track goes to a separate fruityLSD, and each split instrument is on a separate channel. Cannot have more than 16 unique instruments per FruityLSD
		saver.CurGroupWithAbove = false;

		// TODO: If there are 0 or 1 events, don't create automation pls
		// TODO: Option to not use the optimized lists
		if (_volEventsOptimized.Count != 0)
		{
			FLAutomation a = CreateAuto(saver, "Volume", FLAutomation.MyType.Volume, filter, ourChans);
			foreach (MIDIEvent<ControllerMessage> e in _volEventsOptimized)
			{
				a.AddPoint((uint)e.Ticks, VolumeToAutomation(e.Msg.Value));
			}
			saver.AddMIDITrackAuto(a, VolumeToAutomation(DEFAULT_MIDI_VOL));
		}
		if (_panEventsOptimized.Count != 0)
		{
			FLAutomation a = CreateAuto(saver, "Panpot", FLAutomation.MyType.Panpot, filter, ourChans);
			foreach (MIDIEvent<ControllerMessage> e in _panEventsOptimized)
			{
				a.AddPoint((uint)e.Ticks, PanpotToAutomation(e.Msg.Value));
			}
			saver.AddMIDITrackAuto(a, PanpotToAutomation(DEFAULT_MIDI_PAN));
		}
		if (_pitchEventsOptimized.Count != 0)
		{
			double unitsPerCent = 8_192d / (saver.Options.PitchBendRange * 100);

			FLAutomation a = CreateAuto(saver, "Pitch", FLAutomation.MyType.Pitch, filter, ourChans);
			foreach (MIDIEvent<PitchBendMessage> e in _pitchEventsOptimized)
			{
				a.AddPoint((uint)e.Ticks, PitchToAutomation(e.Msg.GetPitchAsInt(), unitsPerCent));
			}
			saver.AddMIDITrackAuto(a, PitchToAutomation(DEFAULT_MIDI_PITCH, unitsPerCent));
		}
		if (outputInstrumentAutos && _programEventsOptimized.Count >= 2)
		{
			FLAutomation a = CreateAuto(saver, "Instrument", FLAutomation.MyType.MIDIProgram, filter, ourChans);
			foreach (MIDIEvent<ProgramChangeMessage> e in _programEventsOptimized)
			{
				a.AddPoint((uint)e.Ticks, ProgramToAutomation(e.Msg.Program));
			}
			saver.AddMIDITrackAuto(a, ProgramToAutomation(DEFAULT_MIDI_PROGRAM));
		}
	}
	private FLAutomation CreateAuto(FLPSaver saver, string type, FLAutomation.MyType flType, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		FLAutomation a = saver.FLP.CreateAutomation($"Track {_trackIndex + 1} {type}", flType, ourChans, filter);
		a.Color = saver.Options.GetAutomationColor(flType);
		return a;
	}

	private static double VolumeToAutomation(byte vol)
	{
		return vol / 127d;
	}
	private static double PanpotToAutomation(byte pan)
	{
		// 0 => 100% left, 64 => center, 127 => 100% right
		// Split the operation to ensure 0.5 is centered
		if (pan <= 64)
		{
			return FLUtils.LerpUnclamped(0, 64, 0, 0.5, pan);
		}
		return FLUtils.LerpUnclamped(64, 127, 0.5, 1, pan);
	}
	private static double PitchToAutomation(int midiUnits, double unitsPerCent)
	{
		// midiUnits is [-8192, 8191]
		// 0 => -4800 cents, 0.5 => +0 cents, 1.0 => 4800 cents
		return FLUtils.LerpUnclamped(-4800, 4800, 0, 1, midiUnits / unitsPerCent);
	}
	private static double ProgramToAutomation(MIDIProgram program)
	{
		return (double)(program + 1) / 128d;
	}
}
