using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLPattern
{
	public readonly List<FLPatternNote> Notes;
	public FLColor3? Color;
	public string? Name;

	public FLPattern()
	{
		Notes = new List<FLPatternNote>();
	}

	public void WritePatternNotes(EndianBinaryWriter w)
	{
		// Must be in order of AbsoluteTick
		Notes.Sort((n1, n2) => n1.AbsoluteTick.CompareTo(n2.AbsoluteTick));

		w.WriteEnum(FLEvent.PatternNotes);
		FLProjectWriter.WriteTextEventLength(w, (uint)Notes.Count * FLPatternNote.LEN);
		foreach (FLPatternNote note in Notes)
		{
			note.Write(w);
		}
	}
	public void WriteColorAndNameIfNecessary(EndianBinaryWriter w, ushort id)
	{
		if (Name is not null)
		{
			if (Color is null)
			{
				throw new Exception("If you supply a name, you must supply a color");
			}

			FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.PatternName, Name + '\0');
		}
		else if (Color is null)
		{
			return;
		}

		FLProjectWriter.WriteWordEvent(w, FLEvent.NewPattern, id);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.PatColor, Color.Value.GetValue());
		// Dunno what these are, but they are always these 3 values no matter what I touch in the color picker.
		// Patterns don't have icons, and the preset name/colors don't affect it, so idk
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Unk_157, uint.MaxValue);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Unk_158, uint.MaxValue);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Unk_164, 0);
	}
}
