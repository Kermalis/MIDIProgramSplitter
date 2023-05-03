using Kermalis.MIDI;
using MIDIProgramSplitter.FLP;
using System;
using System.Collections.Generic;
using System.IO;

namespace MIDIProgramSplitter.CMD;

internal static class Program
{
	private static void Main(string[] args)
	{
		//Test_ReadFLP();
		//Test_WriteFLP();
		//return;

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
	private static void Test_WriteFLP()
	{
		const string OUT = @"C:\Users\Kermalis\Documents\Development\GitHub\MIDIProgramSplitter\TestOUT.flp";

		using (FileStream s = File.Create(OUT))
		{
			var w = new FLProjectWriter();

			w.Channels.Add(new FLChannel("Test Chan 0!", 0, 0));
			w.Channels.Add(new FLChannel("CHAN ONE", 1, MIDIProgram.FrenchHorn));

			w.Automations.Add(new FLAutomation("Test auto", FLAutomation.MyType.Volume, new List<FLChannel> { w.Channels[1] })
			{
				Points =
				{
					new FLAutomation.Point
					{
						Value = 0.5
					},
					new FLAutomation.Point
					{
						AbsoluteTicks = 96,
						Value = 1
					},
				}
			});

			w.Patterns.Add(new FLPattern
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
			});

			w.AddToPlaylist(w.Patterns[0], 0, 96 * 4, w.PlaylistTracks[0]);

			w.Write(s);
		}
	}
}