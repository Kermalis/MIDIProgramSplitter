using FLP;
using Kermalis.MIDI;
using System;
using System.Linq;

namespace MIDIProgramSplitter;

public sealed class FLPSaveOptions
{
	public enum EPatternColorMode : byte
	{
		None,
		Random,
		Track, // TODO
		Instrument, // TODO
	}
	public enum EInsertColorMode : byte
	{
		None,
		Random,
		Track, // TODO
	}
	public enum EAutomationColorMode : byte
	{
		None,
		Random,
		Track, // TODO
	}

	public string DLSPath;
	public float AutomationTrackSize;
	public int PitchBendRange;

	public bool GroupMIDITrackAutomations;
	public bool CollapseAutomationGroups;

	public EPatternColorMode PatternColorMode;
	public EInsertColorMode InsertColorMode;
	public EAutomationColorMode AutomationColorMode;
	// TODO: Channel colors

	public FLPSaveOptions()
	{
		DLSPath = string.Empty;
		AutomationTrackSize = FLPlaylistTrack.SIZE_MIN;
		PitchBendRange = 12;

		GroupMIDITrackAutomations = true;
		CollapseAutomationGroups = true;

		PatternColorMode = EPatternColorMode.Instrument;
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

	public FLColor3? GetPatternColor(MIDIProgram program)
	{
		switch (PatternColorMode)
		{
			case EPatternColorMode.Random: return FLColor3.GetRandom();
			case EPatternColorMode.Instrument: return FLColor3.FromRGB(MIDIUtils.InstrumentColorsRGB[(int)program]);
			case EPatternColorMode.Track: // TODO
			default: return null;
		}
	}
	public FLColor3? GetInsertColor()
	{
		switch (InsertColorMode)
		{
			case EInsertColorMode.Random: return FLColor3.GetRandom();
			case EInsertColorMode.Track: // TODO
			default: return null;
		}
	}
	public FLColor3 GetAutomationColor(FLAutomation.MyType type)
	{
		switch (AutomationColorMode)
		{
			case EAutomationColorMode.Random: return FLColor3.GetRandom();
			case EAutomationColorMode.Track: // TODO
			default: return FLAutomation.GetColor(type);
		}
	}
}
