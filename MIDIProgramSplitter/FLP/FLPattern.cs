using Kermalis.EndianBinaryIO;
using System.Collections.Generic;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLPattern
{
	public readonly List<FLPatternNote> Notes;

	public FLPattern()
	{
		Notes = new List<FLPatternNote>();
	}

	public void WritePatternNotes(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.PatternNotes);
		FLProject.WriteTextEventLength(w, (uint)Notes.Count * FLPatternNote.LEN);
		foreach (FLPatternNote note in Notes)
		{
			note.Write(w);
		}
	}
}
