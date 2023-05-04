using FLP;
using Kermalis.MIDI;
using System;
using System.IO;

namespace MIDIProgramSplitter.CMD;

internal static class Program
{
	private static void Main(string[] args)
	{
		//Test_ReadFLP(); return;

#if DEBUG
		args = new string[]
		{
			//@"E:\Music\MIDIs\Pokemon B2W2\Music\Roaming Legendary Battle format1.mid",
			@"E:\Music\MIDIs\Pokemon B2W2\Music\B2W2KantoChampion format1.mid",
			//@"E:\Music\MIDIs\Pokemon B2W2\Music\Elite4Battle format1.mid",
			//@"E:\Music\MIDIs\Pokemon B2W2\Music\Iris Battle format1.mid",
			//@"E:\Music\MIDIs\Pokemon B2W2\Music\Colress Battle.mid",
			//@"E:\Music\MIDIs\DS Rips\HGSS\BATTLE1\SEQ_GS_VS_TRAINER format1.mid",
			//@"E:\Music\MIDIs\DS Rips\HGSS\BATTLE2\SEQ_GS_VS_RIVAL format1.mid",
			//@"E:\Music\MIDIs\DS Rips\HGSS\BATTLE6\SEQ_GS_VS_RAIKOU format1.mid",
			//@"C:\Users\Kermalis\Documents\Development\GitHub\pokeemerald\sound\songs\midi\mus_vs_aqua_magma.mid",
			//@"C:\Users\Kermalis\Documents\Development\GitHub\pokeemerald\sound\songs\midi\mus_surf.mid",

			@"E:\Music\MIDIs\Split Rips\BWB2W2"
			//@"E:\Music\MIDIs\Split Rips\HGSS"
			//@"E:\Music\MIDIs\Split Rips\RSE"
		};
#endif

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
		const string OUT = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TestOUT.flp";

		using (FileStream fs = File.OpenRead(inFile))
		{
			var inMIDI = new MIDIFile(fs);
			var splitter = new Splitter(inMIDI);
			splitter.SaveMIDI(outFile);
			splitter.SaveFLP(OUT);
		}

		Console.WriteLine();
		Console.WriteLine("Successfully saved \"" + outFile + '\"');

#if DEBUG
		Console.ReadKey();
#endif
	}

	private static void Test_ReadFLP()
	{
		const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TestIn.flp";
		//const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TestOUT.flp";
		//const string IN = @"D:\Music\Projects\Remix\Vs Colress.flp";

		using (FileStream s = File.OpenRead(IN))
		{
			var flp = new FLProjectReader(s);

#if DEBUG && WINDOWS
			WinUtils.Win_SetClipboardString(flp.Log.ToString());
#endif
			;
		}
	}
}