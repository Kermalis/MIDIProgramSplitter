using Kermalis.MIDI;
using System;
using System.IO;

namespace MIDIProgramSplitter;

internal static class Program
{
	private static void Main(string[] args)
	{
		if (args.Length != 2)
		{
			throw new Exception("Must have two arguments"); // TODO: Example usage
		}

		string inFile = args[0];
		string outDir = args[1];
		if (!File.Exists(inFile))
		{
			throw new Exception("File not found: \"" + inFile + '\"');
		}
		if (!Path.Exists(outDir))
		{
			throw new Exception("Output directory not found: \"" + outDir + '\"');
		}
		string outFile = Path.Combine(outDir, Path.GetFileName(inFile));

		using (FileStream fs = File.OpenRead(inFile))
		{
			var inMIDI = new MIDIFile(fs);
			var outMIDI = new OutMIDI(inMIDI);
			outMIDI.Save(outFile);
		}

		Console.WriteLine();
		Console.WriteLine("Successfully saved \"" + outFile + '\"');
	}
}