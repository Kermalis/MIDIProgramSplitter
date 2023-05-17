using FLP;
using Kermalis.MIDI;
using System;
using System.IO;

namespace MIDIProgramSplitter.CMD;

internal static class Program
{
	private static void Main(string[] args)
	{
		if (args.Length < 3)
		{
			ShowUsage();
			return;
		}

		try
		{
			string mode = args[0].ToLowerInvariant();
			switch (mode)
			{
				case "-midsplit":
				{
					Mode_MIDSplit(args.AsSpan(1));
					break;
				}
				case "-flpreport":
				{
					Mode_FLPReport(args.AsSpan(1));
					break;
				}
				case "-midtoflp":
				{
					Mode_MIDToFLP(args.AsSpan(1));
					break;
				}
				default:
				{
					ShowUsage();
					return;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex);
		}
	}

	private static void Mode_MIDSplit(ReadOnlySpan<string> args)
	{
		string inMIDIPath = args[0];

		if (!File.Exists(inMIDIPath))
		{
			Console.WriteLine("File not found: \"" + inMIDIPath + '\"');
			return;
		}

		string outMIDIPath = args[1];
		byte defaultVol = 127;

		try
		{
			for (int i = 2; i < args.Length;)
			{
				string extraArg = args[i++].ToLowerInvariant();
				switch (extraArg)
				{
					case "-defaultvol":
					{
						HandleDefaultVol(args[i++], ref defaultVol);
						break;
					}
					default: throw new Exception();
				}
			}
		}
		catch
		{
			ShowUsage();
			return;
		}

		CreateOutputDir(outMIDIPath);

		MIDIFile inMIDI;
		using (FileStream fs = File.OpenRead(inMIDIPath))
		{
			inMIDI = new MIDIFile(fs);
		}

		var splitter = new Splitter(inMIDI, defaultVol);
		using (FileStream fs = File.Create(outMIDIPath))
		{
			splitter.SaveMIDI(fs);
		}

		Console.WriteLine();
		Console.WriteLine("Successfully saved \"" + outMIDIPath + '\"');
	}
	private static void Mode_FLPReport(ReadOnlySpan<string> args)
	{
		string inFLPPath = args[0];

		if (!File.Exists(inFLPPath))
		{
			Console.WriteLine("File not found: \"" + inFLPPath + '\"');
			return;
		}

		string outTXTPath = args[1];

		CreateOutputDir(outTXTPath);

		FLProjectReader flp;
		using (FileStream fs = File.OpenRead(inFLPPath))
		{
			flp = new FLProjectReader(fs);
		}

		File.WriteAllText(outTXTPath, flp.Log.ToString());
	}
	private static void Mode_MIDToFLP(ReadOnlySpan<string> args)
	{
		string inMIDIPath = args[0];

		if (!File.Exists(inMIDIPath))
		{
			Console.WriteLine("File not found: \"" + inMIDIPath + '\"');
			return;
		}

		string outFLPPath = args[1];
		var options = new FLPSaveOptions
		{
			DefaultMIDIVolume = 127,
		};

		try
		{
			for (int i = 2; i < args.Length;)
			{
				string extraArg = args[i++].ToLowerInvariant();
				switch (extraArg)
				{
					case "-defaultvol":
					{
						HandleDefaultVol(args[i++], ref options.DefaultMIDIVolume);
						break;
					}
					case "-flversioncompat":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "v20_9_2":
							{
								options.FLVersionCompat = FLVersionCompat.V20_9_2__B2963;
								break;
							}
							case "v21_0_3":
							{
								options.FLVersionCompat = FLVersionCompat.V21_0_3__B3517;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					case "-dlspath":
					{
						options.DLSPath = args[i++];
						break;
					}
					case "-pitchbendrange":
					{
						options.PitchBendRange = int.Parse(args[i++]);
						if (options.PitchBendRange is < 1 or > 48)
						{
							throw new Exception();
						}
						break;
					}
					case "-automationtracksize":
					{
						options.AutomationTrackSize = float.Parse(args[i++]);
						if (options.AutomationTrackSize is < FLPlaylistTrack.SIZE_MIN or > FLPlaylistTrack.SIZE_MAX)
						{
							throw new Exception();
						}
						break;
					}
					case "-automationgrouping":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "groupbychannel":
							{
								options.AutomationGrouping = FLPSaveOptions.AutomationGroupMode.GroupByChannel;
								break;
							}
							case "groupall":
							{
								options.AutomationGrouping = FLPSaveOptions.AutomationGroupMode.GroupAll;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					case "-collapseautomationgroups":
					{
						options.CollapseAutomationGroups = bool.Parse(args[i++]);
						break;
					}
					case "-appendinstrumentnamestopatterns":
					{
						options.AppendInstrumentNamesToPatterns = bool.Parse(args[i++]);
						break;
					}
					case "-instrumenttrackcoloring":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "random":
							{
								options.InstrumentTrackColoring = FLPSaveOptions.InstrumentTrackColorMode.Random;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					case "-automationtrackcoloring":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "random":
							{
								options.AutomationTrackColoring = FLPSaveOptions.AutomationTrackColorMode.Random;
								break;
							}
							case "instrumenttrack":
							{
								options.AutomationTrackColoring = FLPSaveOptions.AutomationTrackColorMode.InstrumentTrack;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					case "-midioutcoloring":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "random":
							{
								options.MIDIOutColoring = FLPSaveOptions.MIDIOutColorMode.Random;
								break;
							}
							case "instrumenttrack":
							{
								options.MIDIOutColoring = FLPSaveOptions.MIDIOutColorMode.InstrumentTrack;
								break;
							}
							case "instrument":
							{
								options.MIDIOutColoring = FLPSaveOptions.MIDIOutColorMode.Instrument;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					case "-patterncoloring":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "random":
							{
								options.PatternColoring = FLPSaveOptions.PatternColorMode.Random;
								break;
							}
							case "instrumenttrack":
							{
								options.PatternColoring = FLPSaveOptions.PatternColorMode.InstrumentTrack;
								break;
							}
							case "instrument":
							{
								options.PatternColoring = FLPSaveOptions.PatternColorMode.Instrument;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					case "-insertcoloring":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "random":
							{
								options.InsertColoring = FLPSaveOptions.InsertColorMode.Random;
								break;
							}
							case "instrumenttrack":
							{
								options.InsertColoring = FLPSaveOptions.InsertColorMode.InstrumentTrack;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					case "-automationcoloring":
					{
						switch (args[i++].ToLowerInvariant())
						{
							case "random":
							{
								options.AutomationColoring = FLPSaveOptions.AutomationColorMode.Random;
								break;
							}
							case "automationtrack":
							{
								options.AutomationColoring = FLPSaveOptions.AutomationColorMode.AutomationTrack;
								break;
							}
							default: throw new Exception();
						}
						break;
					}
					default: throw new Exception();
				}
			}
		}
		catch
		{
			ShowUsage();
			return;
		}

		CreateOutputDir(outFLPPath);

		MIDIFile inMIDI;
		using (FileStream fs = File.OpenRead(inMIDIPath))
		{
			inMIDI = new MIDIFile(fs);
		}

		var splitter = new Splitter(inMIDI, options.DefaultMIDIVolume);
		using (FileStream fs = File.Create(outFLPPath))
		{
			splitter.SaveFLP(fs, options);
		}

		Console.WriteLine();
		Console.WriteLine("Successfully saved \"" + outFLPPath + '\"');
	}

	private static void HandleDefaultVol(string s, ref byte defaultVol)
	{
		defaultVol = byte.Parse(s);
		if (defaultVol is < 1 or > 127)
		{
			throw new Exception();
		}
	}
	private static void CreateOutputDir(string outFile)
	{
		string test = Path.GetDirectoryName(outFile)!;
		Console.WriteLine(test);
		Directory.CreateDirectory(test);
	}

	private static void ShowUsage()
	{
		Console.WriteLine("######################### MIDI Program Splitter by Kermalis #########################");
		Console.WriteLine("Example usage:");

		Console.WriteLine("  MIDIProgramSplitter.exe -MIDSplit \"InputMIDIPath.mid\" \"OutputFLPPath.flp\" -DefaultVol 100");
		Console.WriteLine("    Optional args:");
		Console.WriteLine("      -DefaultVol [0,127]");
		Console.WriteLine("    Default args:");
		Console.WriteLine("      DefaultVol = 127");
		Console.WriteLine();

		Console.WriteLine("  MIDIProgramSplitter.exe -FLPReport \"InputFLPPath.flp\" \"OutputTXTPath.txt\"");
		Console.WriteLine();

		Console.WriteLine("  MIDIProgramSplitter.exe -MIDToFLP \"InputMIDIPath.mid\" \"OutputFLPPath.flp\" -DefaultVol 100 -FLVersionCompat V21_0_3 -DLSPath \"PathToDLS.dls\"");
		Console.WriteLine("    Optional args:");
		Console.WriteLine("      -DefaultVol [0,127]");
		Console.WriteLine("      -FLVersionCompat {V20_9_2 or V21_0_3}");
		Console.WriteLine("      -DLSPath \"PathToDLS.dls\"");
		Console.WriteLine("      -PitchBendRange [1,48]");
		Console.WriteLine("      -AutomationTrackSize [0.0,25.9249992370605]");
		Console.WriteLine("      -AutomationGrouping {GroupByChannel or GroupAll}");
		Console.WriteLine("      -CollapseAutomationGroups {False}");
		Console.WriteLine("      -AppendInstrumentNamesToPatterns {False}");
		Console.WriteLine("      -InstrumentTrackColoring {Random}");
		Console.WriteLine("      -AutomationTrackColoring {Random or InstrumentTrack}");
		Console.WriteLine("      -MIDIOutColoring {None or Random or InstrumentTrack}");
		Console.WriteLine("      -PatternColoring {None or Random or InstrumentTrack}");
		Console.WriteLine("      -InsertColoring {Random or InstrumentTrack}");
		Console.WriteLine("      -AutomationColoring {Random or AutomationTrack}");
		Console.WriteLine("    Default args:");
		Console.WriteLine("      DefaultVol = 127");
		Console.WriteLine("      FLVersionCompat = V20_9_2");
		Console.WriteLine("      PitchBendRange = 12");
		Console.WriteLine("      AutomationTrackSize = 0.0");

		Console.WriteLine("#########################                                   #########################");
	}
}