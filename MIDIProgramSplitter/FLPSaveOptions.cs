using FLP;
using Kermalis.MIDI;
using System;
using System.Linq;

namespace MIDIProgramSplitter;

public sealed class FLPSaveOptions
{
	public enum AutomationGroupMode : byte
	{
		None,
		GroupByChannel,
		GroupAll,
	}
	public enum InstrumentTrackColorMode : byte
	{
		None,
		Random,
	}
	public enum AutomationTrackColorMode : byte
	{
		None,
		Random,
		InstrumentTrack,
	}
	public enum MIDIOutColorMode : byte
	{
		None,
		Random,
		InstrumentTrack,
		Instrument,
	}
	public enum PatternColorMode : byte
	{
		None,
		Random,
		InstrumentTrack,
		Instrument,
	}
	public enum InsertColorMode : byte
	{
		None,
		Random,
		InstrumentTrack,
	}
	public enum AutomationColorMode : byte
	{
		None,
		Random,
		AutomationTrack,
	}

	public FLVersionCompat FLVersionCompat;

	public string DLSPath;

	public int PitchBendRange;
	public byte DefaultMIDIVolume;

	public float AutomationTrackSize;
	public AutomationGroupMode AutomationGrouping;
	public bool CollapseAutomationGroups;

	public bool AppendInstrumentNamesToPatterns;

	public InstrumentTrackColorMode InstrumentTrackColoring;
	public AutomationTrackColorMode AutomationTrackColoring;
	public MIDIOutColorMode MIDIOutColoring;
	public PatternColorMode PatternColoring;
	public InsertColorMode InsertColoring;
	public AutomationColorMode AutomationColoring;
	// TODO: Channel colors
	// TODO: Option to open FL project after export

	public FLPSaveOptions()
	{
		FLVersionCompat = FLVersionCompat.V20_9_2__B2963;

		DLSPath = string.Empty;

		PitchBendRange = 12;
		DefaultMIDIVolume = 127; // SDAT 127, MP2K 100. I believe MIDI defaults to 127

		AutomationTrackSize = FLPlaylistTrack.SIZE_MIN;
		AutomationGrouping = AutomationGroupMode.GroupAll;
		CollapseAutomationGroups = true;

		AppendInstrumentNamesToPatterns = true;

		InstrumentTrackColoring = InstrumentTrackColorMode.None;
		AutomationTrackColoring = AutomationTrackColorMode.None;
		MIDIOutColoring = MIDIOutColorMode.Instrument;
		PatternColoring = PatternColorMode.Instrument;
		InsertColoring = InsertColorMode.None;
		AutomationColoring = AutomationColorMode.None;
	}

	internal void Validate()
	{
		if (DLSPath.Length > 255)
		{
			throw new Exception($"DLS Path must be 255 characters or less. This one was {DLSPath.Length}.");
		}
		if (!DLSPath.All(char.IsAscii))
		{
			throw new Exception("DLS Path must consist of only ASCII characters.");
		}
	}

	public FLColor3 GetInstrumentTrackColor()
	{
		switch (InstrumentTrackColoring)
		{
			case InstrumentTrackColorMode.Random: return FLColor3.GetRandom();
			default: return FLPlaylistTrack.DefaultColor;
		}
	}
	public FLColor3 GetAutomationTrackColor(FLPlaylistTrack instTrack)
	{
		switch (AutomationTrackColoring)
		{
			case AutomationTrackColorMode.Random: return FLColor3.GetRandom();
			case AutomationTrackColorMode.InstrumentTrack: return instTrack.Color;
			default: return FLPlaylistTrack.DefaultColor;
		}
	}
	public FLColor3 GetMIDIOutColor(MIDIProgram program, FLPlaylistTrack instTrack)
	{
		switch (MIDIOutColoring)
		{
			case MIDIOutColorMode.Random: return FLColor3.GetRandom();
			case MIDIOutColorMode.Instrument: return FLColor3.FromRGB(MIDIUtils.InstrumentColorsRGB[(int)program]);
			case MIDIOutColorMode.InstrumentTrack: return instTrack.Color;
			default: return FLPattern.DefaultColor;
		}
	}
	public FLColor3 GetPatternColor(MIDIProgram program, FLPlaylistTrack instTrack)
	{
		switch (PatternColoring)
		{
			case PatternColorMode.Random: return FLColor3.GetRandom();
			case PatternColorMode.Instrument: return FLColor3.FromRGB(MIDIUtils.InstrumentColorsRGB[(int)program]);
			case PatternColorMode.InstrumentTrack: return instTrack.Color;
			default: return FLPattern.DefaultColor;
		}
	}
	public FLColor3 GetInsertColor(FLPlaylistTrack instTrack)
	{
		switch (InsertColoring)
		{
			case InsertColorMode.Random: return FLColor3.GetRandom();
			case InsertColorMode.InstrumentTrack: return instTrack.Color;
			default: return FLInsert.DefaultColor;
		}
	}
	public FLColor3 GetAutomationColor(FLAutomation.MyType type, FLPlaylistTrack autoTrack)
	{
		switch (AutomationColoring)
		{
			case AutomationColorMode.Random: return FLColor3.GetRandom();
			case AutomationColorMode.AutomationTrack: return autoTrack.Color;
			default: return FLAutomation.GetDefaultColor(type);
		}
	}
}
