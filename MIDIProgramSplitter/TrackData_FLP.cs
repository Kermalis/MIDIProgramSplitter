using Kermalis.MIDI;
using MIDIProgramSplitter.FLP;
using System.Collections.Generic;

namespace MIDIProgramSplitter;

partial class TrackData
{
	public void FLP_AddNewTracks(FLProjectWriter w, uint maxTicks)
	{
		if (_newTracks is null)
		{
			return; // TODO: Don't ignore automation?
		}

		Dictionary<MIDIProgram, NewTrack> dict = _newTracks.Dict;
		List<FLChannel> ourChans = FLP_CreateChannels(w, dict);
		FLP_CreatePatterns(w, ourChans, dict);
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
	private void FLP_CreatePatterns(FLProjectWriter w, List<FLChannel> ourChans, Dictionary<MIDIProgram, NewTrack> dict)
	{
		FLPlaylistTrack pTrack = w.PlaylistTracks[_trackIndex];
		pTrack.Name = "T" + (_trackIndex + 1) + 'C' + (_trackChannel + 1);

		int ourChanID = 0;
		int patID = 1;
		foreach (KeyValuePair<MIDIProgram, NewTrack> kvp in dict)
		{
			MIDIProgram program = kvp.Key;
			NewTrack newT = kvp.Value;

			FLChannel ourChan = ourChans[ourChanID++];
			ushort chanID = (ushort)w.Channels.IndexOf(ourChan);

			// TODO: In johto trainer, the first pattern of the first track is going to the bottom...
			foreach (NewTrackPattern newP in newT.Patterns)
			{
				string name = string.Format("{0} #{1} - {2}", pTrack.Name, patID++, program);
				newP.AddToFLP(w, chanID, pTrack, name);
			}
		}
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
