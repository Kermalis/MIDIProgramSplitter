using Kermalis.EndianBinaryIO;
using System.Collections.Generic;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLPattern
{
	public readonly List<FLPatternNote> Notes;
	public FLColor3? Color;

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
	public void WriteColorIfNecessary(EndianBinaryWriter w, ushort id)
	{
		if (Color is null)
		{
			return;
		}

		FLProjectWriter.WriteWordEvent(w, FLEvent.NewPattern, id);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.PatColor, Color.Value.GetValue());
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Unk_157, uint.MaxValue);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Unk_158, uint.MaxValue);
		FLProjectWriter.WriteDWordEvent(w, FLEvent.Unk_164, 0);
	}
}
