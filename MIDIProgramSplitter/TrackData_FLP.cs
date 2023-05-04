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
		var ourChans = new List<FLChannel>();
		foreach (NewTrack newT in dict.Values)
		{
			ourChans.Add(w.CreateChannel(newT.Name, _trackChannel, newT.Program, filter));
		}
		return ourChans;
	}
	private void FLP_CreatePatterns(FLProjectWriter w, string name, List<FLChannel> ourChans, Dictionary<MIDIProgram, NewTrack> dict)
	{
		FLArrangement arrang = w.Arrangements[0];
		FLPlaylistTrack pTrack = arrang.PlaylistTracks[_trackIndex];
		pTrack.Name = name; // TODO: option to color based on instrument

		int ourChanID = 0;
		int ourPatID = 1;
		foreach (NewTrack newT in dict.Values)
		{
			FLChannel ourChan = ourChans[ourChanID++];

			// TODO: Why is #3 before #2 sometimes etc
			// Is it the dict order?
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
		bool groupWithAbove = false;

		// TODO: If there are 0 or 1 events, don't create automation pls
		if (_volEvents.Count != 0)
		{
			FLAutomation a = CreateAuto(w, "Volume", FLAutomation.MyType.Volume, filter, ourChans);
			foreach (MIDIEvent e in _volEvents)
			{
				byte v = ((ControllerMessage)e.Message).Value;
				a.AddPoint((uint)e.Ticks, v / 127d);
			}
			AddAuto(w, a, maxTicks, 1d, ref automationTrackIndex, ref groupWithAbove); // I believe MIDI defaults to max channel volume
		}
		if (_panEvents.Count != 0)
		{
			FLAutomation a = CreateAuto(w, "Panpot", FLAutomation.MyType.Panpot, filter, ourChans);
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
			AddAuto(w, a, maxTicks, 0.5d, ref automationTrackIndex, ref groupWithAbove); // Centered
		}
		if (_pitchEvents.Count != 0)
		{
			FLAutomation a = CreateAuto(w, "Pitch", FLAutomation.MyType.Pitch, filter, ourChans);
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
			AddAuto(w, a, maxTicks, 0.5d, ref automationTrackIndex, ref groupWithAbove); // +0 cents
		}
		if (_programEvents.Count != 0)
		{
			FLAutomation a = CreateAuto(w, "Instrument", FLAutomation.MyType.MIDIProgram, filter, ourChans);
			foreach (MIDIEvent e in _programEvents)
			{
				byte v = (byte)(((ProgramChangeMessage)e.Message).Program + 1);
				a.AddPoint((uint)e.Ticks, v / 128d);
			}
			AddAuto(w, a, maxTicks, 1 / 128d, ref automationTrackIndex, ref groupWithAbove); // Program 0
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
}
