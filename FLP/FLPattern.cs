using Kermalis.EndianBinaryIO;
using System.Collections.Generic;

namespace FLP;

public sealed class FLPattern
{
	public static FLColor3 DefaultColor => new(72, 81, 86);

	internal ushort Index;
	internal ushort ID => (ushort)(Index + 1);

	public readonly List<FLPatternNote> Notes;
	public FLColor3 Color;
	public string? Name;

	internal FLPattern()
	{
		Notes = new List<FLPatternNote>();
		Color = DefaultColor;
	}

	internal void WritePatternNotes(EndianBinaryWriter w)
	{
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewPattern, ID);

		// Must be in order of AbsoluteTick
		Notes.Sort((n1, n2) => n1.AbsoluteTick.CompareTo(n2.AbsoluteTick));

		w.WriteEnum(FLEvent.PatternNotes);
		FLProjectWriter.WriteArrayEventLength(w, (uint)Notes.Count * FLPatternNote.LEN);
		foreach (FLPatternNote note in Notes)
		{
			note.Write(w);
		}
	}
	internal void WriteColorAndNameIfNecessary(EndianBinaryWriter w)
	{
		// If you supply a name, you must supply a color
		// But a color can be here with no name
		if (Name is null && Color.Equals(DefaultColor))
		{
			return;
		}

		FLProjectWriter.Write16BitEvent(w, FLEvent.NewPattern, ID);

		if (Name is not null)
		{
			FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.PatternName, Name + '\0');
		}
		FLProjectWriter.Write32BitEvent(w, FLEvent.PatternColor, Color.GetFLValue());

		// Dunno what these are, but they are always these 3 values no matter what I touch in the color picker.
		// Patterns don't have icons, and the preset name/colors don't affect it, so idk
		FLProjectWriter.Write32BitEvent(w, FLEvent.Unk_157, uint.MaxValue);
		FLProjectWriter.Write32BitEvent(w, FLEvent.Unk_158, uint.MaxValue);
		FLProjectWriter.Write32BitEvent(w, FLEvent.Unk_164, 0);
	}
}
