using FLP;
using Kermalis.MIDI;

namespace MIDIProgramSplitter;

internal sealed class FLPSaver
{
	public readonly FLProjectWriter FLP;
	public readonly FLPSaveOptions Options;

	public uint MaxTicks;

	public FLChannelFilter? AutoFilter;
	public int AutomationTrackIndex;

	public MIDIEvent<MetaMessage>? FirstTempo;
	public FLAutomation? TempoAuto;

	public MIDIEvent<MetaMessage>? FirstTimeSig;
	public bool CreatedFirstTimeSigMarker;

	public bool CurGroupWithAbove;

	public FLPSaver(FLPSaveOptions o)
	{
		FLP = new FLProjectWriter();
		Options = o;
	}

	public void HandleTempo(MIDIEvent<MetaMessage> e)
	{
		// Some will come out with .999 or .001 for example, but that's fine. No way to really deal with it automatically
		// TODO: Remove unnecessary events
		e.Msg.ReadTempoMessage(out _, out decimal bpm);
		if (FirstTempo is null)
		{
			FLP.CurrentTempo = bpm;
			FirstTempo = e;
			return;
		}

		// This is the 2nd or after change. 2nd will create the autoclip
		if (TempoAuto is null)
		{
			AutoFilter ??= FLP.CreateAutomationFilter();
			TempoAuto = FLP.CreateTempoAutomation("Tempo", AutoFilter);

			FLArrangement arrang = FLP.Arrangements[0];
			FLPlaylistTrack track = arrang.PlaylistTracks[AutomationTrackIndex++];
			track.Size = Options.AutomationTrackSize;
			arrang.AddToPlaylist(TempoAuto, 0, MaxTicks, track);

			TempoAuto.AddTempoPoint((uint)FirstTempo.Ticks, FLP.CurrentTempo);
		}
		TempoAuto.AddTempoPoint((uint)e.Ticks, bpm);
	}
	public void HandleTimeSig(MIDIEvent<MetaMessage> e)
	{
		// Keeping all time signature events
		e.Msg.ReadTimeSignatureMessage(out byte num, out byte denom, out _, out _);
		if (FirstTimeSig is null)
		{
			FLP.TimeSigNumerator = num;
			FLP.TimeSigDenominator = denom;
			FirstTimeSig = e;
			return;
		}

		FLArrangement arrang = FLP.Arrangements[0];

		// This is the 2nd or after change. 2nd will create the marker for #1 and #2
		if (!CreatedFirstTimeSigMarker)
		{
			CreatedFirstTimeSigMarker = true;
			arrang.AddTimeSigMarker((uint)FirstTimeSig.Ticks, FLP.TimeSigNumerator, FLP.TimeSigDenominator);
		}
		arrang.AddTimeSigMarker((uint)e.Ticks, num, denom);
	}

	public void AddMIDITrackAuto(FLAutomation a, double defaultVal)
	{
		a.PadPoints(MaxTicks, defaultVal);

		FLArrangement arrang = FLP.Arrangements[0];
		FLPlaylistTrack track = arrang.PlaylistTracks[AutomationTrackIndex];
		arrang.AddToPlaylist(a, 0, MaxTicks, track);

		track.Size = Options.AutomationTrackSize;

		if (Options.GroupMIDITrackAutomations)
		{
			track.GroupWithAbove = CurGroupWithAbove;

			if (CurGroupWithAbove && Options.CollapseAutomationGroups)
			{
				// Parent of the group
				FLPlaylistTrack prev = arrang.PlaylistTracks[AutomationTrackIndex - 1];
				if (!prev.GroupWithAbove)
				{
					prev.IsGroupCollapsed = true;
				}
			}

			CurGroupWithAbove = true;
		}

		AutomationTrackIndex++;
	}
}
