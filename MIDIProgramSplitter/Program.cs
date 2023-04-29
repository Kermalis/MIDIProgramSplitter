using Kermalis.MIDI;
using MIDIProgramSplitter.FLP;
using System;
using System.IO;

namespace MIDIProgramSplitter;

internal static class Program
{
	private static void Main(string[] args)
	{
		Test_ReadFLP();
		//Test_WriteFLP();
		return;

#if DEBUG
		args = new string[]
		{
			@"E:\Music\MIDIs\Pokemon B2W2\Music\Colress Battle.mid",
			@"E:\Music\MIDIs\Split Rips\BWB2W2"
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

		using (FileStream fs = File.OpenRead(inFile))
		{
			var inMIDI = new MIDIFile(fs);
			var outMIDI = new OutMIDI(inMIDI);
			outMIDI.Save(outFile);
		}

		Console.WriteLine();
		Console.WriteLine("Successfully saved \"" + outFile + '\"');

#if DEBUG
		Console.ReadKey();
#endif
	}
	private static void Test_ReadFLP()
	{
		//const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\Empty.flp";
		//const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\EmptyWithFruityLSD.flp";
		//const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\EmptyWithTwoMIDIOut.flp";
		//const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TwoMIDIOut_D5InPat1.flp";
		const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TwoMIDIOut_D5Cs5InPat1.flp";
		//const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\Test.flp";
		//const string IN = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\Test2.flp";
		//const string IN = @"D:\Music\Projects\Remix\Vs Colress.flp";

		using (FileStream s = File.OpenRead(IN))
		{
			var flp = new FLProject(s);
			;
		}
	}
	private static void Test_WriteFLP()
	{
		const string OUT = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TestOUT.flp";

		using (FileStream s = File.Create(OUT))
		{
			var chans = new FLChannel[]
			{
				new FLChannel("Test Chan 0!", 0, 0),
				new FLChannel("CHAN ONE", 1, 60),
			};
			var autos = new FLAutomation[]
			{
				//
			};
			var pats = new FLPattern[]
			{
				new FLPattern
				{
					Notes = // Must be in order of AbsoluteTick
					{
						new FLPatternNote
						{
							AbsoluteTick = 0,
							Channel = 1,
							DurationTicks = 48,
							Key = 60,
							Velocity = 0x80,
						},
						new FLPatternNote
						{
							AbsoluteTick = 48,
							Channel = 0,
							DurationTicks = 48,
							Key = 62,
							Velocity = 0x64,
						},
					}
				}
			};
			var plItems = new FLPlaylistItem[]
			{
				new FLPlaylistItem
				{
					AbsoluteTick = 0,
					DurationTicks = 96 * 4,
					Pattern1Indexed = 1,
					PlaylistTrack1Indexed = 1,
				}
			};
			FLProject.Write(s, chans, autos, pats, plItems);
		}
	}
}