using FLP;
using Kermalis.MIDI;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

partial class TrackData
{
	public void FLP_AddNewTracks(FLProjectWriter w, uint maxTicks, ref int automationTrackIndex)
	{
		if (_newTracks is null)
		{
			return; // TODO: Don't ignore automation?
		}

		Dictionary<MIDIProgram, NewTrack> dict = _newTracks.Dict;
		string name = string.Format("T{0:D2}C{1:D2}", _trackIndex + 1, _trackChannel + 1);
		FLChannelFilter f = w.CreateChannelFilter(name);
		List<FLChannel> ourChans = FLP_CreateChannels(w, f, dict);
		FLP_CreatePatterns(w, name, ourChans, dict);
		FLP_CreateAutomations(w, f, ourChans, maxTicks, ref automationTrackIndex);
	}
	private List<FLChannel> FLP_CreateChannels(FLProjectWriter w, FLChannelFilter filter, Dictionary<MIDIProgram, NewTrack> dict)
	{
		byte midiChan = _trackChannel;
		byte midiBank = (byte)(_trackIndex - 1); // Meta track doesn't have one

		var ourChans = new List<FLChannel>();
		foreach (NewTrack newT in dict.Values)
		{
			ourChans.Add(w.CreateChannel(newT.Name, midiChan, midiBank, newT.Program, filter));
		}
		return ourChans;
	}
	private void FLP_CreatePatterns(FLProjectWriter w, string name, List<FLChannel> ourChans, Dictionary<MIDIProgram, NewTrack> dict)
	{
		FLArrangement arrang = w.Arrangements[0];
		FLPlaylistTrack pTrack = arrang.PlaylistTracks[_trackIndex - 1]; // Meta track won't have one
		pTrack.Name = name; // TODO: option to color based on instrument

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
				newP.AddToFLP(w, ourChan, pTrack, pName);
			}
		}
	}
	private void FLP_CreateAutomations(FLProjectWriter w, FLChannelFilter filter, List<FLChannel> ourChans, uint maxTicks,
		ref int automationTrackIndex)
	{
		bool outputInstrumentAutos = true; // TODO: Make it an option. Don't need instrument autos if every track goes to a separate fruityLSD, and each split instrument is on a separate channel. Cannot have more than 16 unique instruments per FruityLSD
		bool groupWithAbove = false;

		// TODO: If there are 0 or 1 events, don't create automation pls
		if (_volEvents.Count != 0)
		{
			FLAutomation a = CreateAuto(w, "Volume", FLAutomation.MyType.Volume, filter, ourChans);
			foreach (MIDIEvent e in _volEvents)
			{
				a.AddPoint((uint)e.Ticks, VolumeToAutomation(((ControllerMessage)e.Message).Value));
			}
			AddAuto(w, a, maxTicks, VolumeToAutomation(127), ref automationTrackIndex, ref groupWithAbove); // I believe MIDI defaults to max channel volume
		}
		if (_panEvents.Count != 0)
		{
			FLAutomation a = CreateAuto(w, "Panpot", FLAutomation.MyType.Panpot, filter, ourChans);
			foreach (MIDIEvent e in _panEvents)
			{
				a.AddPoint((uint)e.Ticks, PanpotToAutomation(((ControllerMessage)e.Message).Value));
			}
			AddAuto(w, a, maxTicks, PanpotToAutomation(64), ref automationTrackIndex, ref groupWithAbove);
		}
		if (_pitchEvents.Count != 0)
		{
			int pitchBendRange = 12;
			double unitsPerCent = 8_192d / (pitchBendRange * 100);

			FLAutomation a = CreateAuto(w, "Pitch", FLAutomation.MyType.Pitch, filter, ourChans);
			foreach (MIDIEvent e in _pitchEvents)
			{
				a.AddPoint((uint)e.Ticks, PitchToAutomation(((PitchBendMessage)e.Message).GetPitchAsInt(), unitsPerCent));
			}
			AddAuto(w, a, maxTicks, PitchToAutomation(0, unitsPerCent), ref automationTrackIndex, ref groupWithAbove);
		}
		if (outputInstrumentAutos && _programEvents.Count >= 2)
		{
			FLAutomation a = CreateAuto(w, "Instrument", FLAutomation.MyType.MIDIProgram, filter, ourChans);
			foreach (MIDIEvent e in _programEvents)
			{
				a.AddPoint((uint)e.Ticks, ProgramToAutomation(((ProgramChangeMessage)e.Message).Program));
			}
			AddAuto(w, a, maxTicks, ProgramToAutomation(0), ref automationTrackIndex, ref groupWithAbove);
		}
	}
	private FLAutomation CreateAuto(FLProjectWriter w, string type, FLAutomation.MyType flType, FLChannelFilter filter, List<FLChannel> ourChans)
	{
		FLAutomation a = w.CreateAutomation($"Track {_trackIndex + 1} {type}", flType, ourChans, filter);
		a.Color = FLColor3.GetRandom();
		return a;
	}
	private static void AddAuto(FLProjectWriter w, FLAutomation a, uint maxTicks, double defaultVal,
		ref int automationTrackIndex, ref bool groupWithAbove)
	{
		a.PadPoints(maxTicks, defaultVal);

		FLArrangement arrang = w.Arrangements[0];
		FLPlaylistTrack track = arrang.PlaylistTracks[automationTrackIndex];
		arrang.AddToPlaylist(a, 0, maxTicks, track);

		track.Size = 1f / 3;
		track.GroupWithAbove = groupWithAbove;
		if (groupWithAbove)
		{
			// Parent of the group
			FLPlaylistTrack prev = arrang.PlaylistTracks[automationTrackIndex - 1];
			if (!prev.GroupWithAbove)
			{
				prev.IsGroupCollapsed = true;
			}
		}
		else
		{
			groupWithAbove = true;
		}

		automationTrackIndex++;
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
			return Utils.LerpUnclamped(0, 64, 0, 0.5, pan);
		}
		return Utils.LerpUnclamped(64, 127, 0.5, 1, pan);
	}
	private static double PitchToAutomation(int midiUnits, double unitsPerCent)
	{
		// midiUnits is [-8192, 8191]
		// 0 => -4800 cents, 0.5 => +0 cents, 1.0 => 4800 cents
		return Utils.LerpUnclamped(-4800, 4800, 0, 1, midiUnits / unitsPerCent);
	}
	private static double ProgramToAutomation(MIDIProgram program)
	{
		return (double)(program + 1) / 128d;
	}
}
