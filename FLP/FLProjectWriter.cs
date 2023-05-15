using Kermalis.EndianBinaryIO;
using Kermalis.MIDI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FLP;

public sealed class FLProjectWriter
{
	private static ReadOnlySpan<byte> MIDIInfo0 => new byte[20]
	{
		0x01, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x01,
		0x90, // 144
		0xFF, 0x0F,
		0x04, 0x00, 0x00, 0x00,
		0xD5, 0x01, // 469
		0x00, 0x00
	};
	private static ReadOnlySpan<byte> MIDIInfo1 => new byte[20]
	{
		0xFD, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x80, // 128
		0x90, // 144
		0xFF, 0x0F,
		0x04, 0x00, 0x00, 0x00,
		0xD5, 0x01, // 469
		0x00, 0x00
	};
	private static ReadOnlySpan<byte> MIDIInfo2 => new byte[20]
	{
		0xFF, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00,
		0x04, // 4
		0x00,
		0xFF, 0x0F,
		0x04, 0x00, 0x00, 0x00,
		0x00, 0xFE,
		0xFF, 0xFF
	};

	public readonly FLInsert[] Inserts;
	public readonly List<FLChannelFilter> ChannelFilters;
	public readonly List<FLChannel> Channels;
	public readonly List<FLAutomation> Automations;
	public readonly List<FLPattern> Patterns;
	public readonly List<FLArrangement> Arrangements;
	public FLVersionCompat VersionCompat;
	public ushort PPQN;
	public decimal CurrentTempo;
	public byte TimeSigNumerator;
	public byte TimeSigDenominator;
	public FLChannelFilter? SelectedChannelFilter;
	public FLArrangement? SelectedArrangement;

	public FLProjectWriter()
	{
		// FL Default
		PPQN = 96;
		CurrentTempo = 140;
		TimeSigNumerator = 4;
		TimeSigDenominator = 4;

		ChannelFilters = new List<FLChannelFilter>();
		Channels = new List<FLChannel>();
		Automations = new List<FLAutomation>();
		Patterns = new List<FLPattern>();
		Arrangements = new List<FLArrangement>(1)
		{
			new FLArrangement("Arrangement"),
		};

		Inserts = new FLInsert[127];
		for (byte i = 0; i < 127; i++)
		{
			Inserts[i] = new FLInsert(i);
		}
	}

	public FLChannelFilter CreateUnsortedFilter()
	{
		return CreateChannelFilter("Unsorted");
	}
	public FLChannelFilter CreateAutomationFilter()
	{
		return CreateChannelFilter("Automation");
	}
	public FLChannelFilter CreateChannelFilter(string name)
	{
		var f = new FLChannelFilter(name);
		ChannelFilters.Add(f);
		return f;
	}

	public FLChannel CreateChannel(string name, byte midiChan, byte midiBank, MIDIProgram midiProgram, FLChannelFilter filter)
	{
		var c = new FLChannel(name, midiChan, midiBank, midiProgram, filter);
		Channels.Add(c);
		return c;
	}
	public FLAutomation CreateTempoAutomation(string name, FLChannelFilter filter)
	{
		var a = new FLAutomation(name, FLAutomation.MyType.Tempo, null, filter);
		Automations.Add(a);
		return a;
	}
	public FLAutomation CreateAutomation(string name, FLAutomation.MyType type, List<FLChannel> targets, FLChannelFilter filter)
	{
		var a = new FLAutomation(name, type, targets, filter);
		Automations.Add(a);
		return a;
	}
	public FLAutomation CreateAutomation(string name, FLAutomation.MyType type, FLChannel target, FLChannelFilter filter)
	{
		var a = new FLAutomation(name, type, new List<FLChannel>(1) { target }, filter);
		Automations.Add(a);
		return a;
	}

	public FLPattern CreatePattern()
	{
		var p = new FLPattern();
		Patterns.Add(p);
		return p;
	}

	public void Write(Stream s)
	{
		var w = new EndianBinaryWriter(s, ascii: true);

		FirstPassAssignIDs();

		WriteHeaderChunk(w);
		WriteDataChunk(w);
	}
	private void FirstPassAssignIDs()
	{
		// Channel Filters must be alphabetical. Even if I don't sort them, they will be opened in the wrong order
		ChannelFilters.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
		for (ushort i = 0; i < ChannelFilters.Count; i++)
		{
			ChannelFilters[i].Index = i;
		}
		ushort chanIndex = 0;
		foreach (FLChannel c in Channels)
		{
			c.Index = chanIndex++;
		}
		foreach (FLAutomation a in Automations)
		{
			a.Index = chanIndex++;
		}
		for (ushort i = 0; i < Patterns.Count; i++)
		{
			Patterns[i].Index = i;
		}
		for (ushort i = 0; i < Arrangements.Count; i++)
		{
			Arrangements[i].Index = i;
		}
	}
	private void WriteHeaderChunk(EndianBinaryWriter w)
	{
		w.WriteChars("FLhd");
		w.WriteUInt32(6); // Length
		w.WriteUInt16(0); // Format
		w.WriteUInt16((ushort)(Channels.Count + Automations.Count));
		w.WriteUInt16(PPQN);
	}
	private void WriteDataChunk(EndianBinaryWriter w)
	{
		w.WriteChars("FLdt");

		long dataLenOffset = w.Stream.Position;
		w.WriteUInt32(0); // Write length later

		WriteProjectInfo(w);
		WriteChannels(w);
		foreach (FLArrangement a in Arrangements)
		{
			a.Write(w, VersionCompat);
		}
		WriteMoreStuffIDK(w);
		WriteMixer(w);

		// Write chunk length
		uint length = (uint)(w.Stream.Length - (dataLenOffset + 4));
		w.Stream.Position = dataLenOffset;
		w.WriteUInt32(length);
	}

	internal static void Write8BitEvent(EndianBinaryWriter w, FLEvent ev, byte data)
	{
		w.WriteEnum(ev);
		w.WriteByte(data);
	}
	internal static void Write16BitEvent(EndianBinaryWriter w, FLEvent ev, ushort data)
	{
		w.WriteEnum(ev);
		w.WriteUInt16(data);
	}
	internal static void Write32BitEvent(EndianBinaryWriter w, FLEvent ev, uint data)
	{
		w.WriteEnum(ev);
		w.WriteUInt32(data);
	}
	internal static void WriteArrayEventLength(EndianBinaryWriter w, uint length)
	{
		// TODO: How many bytes can this len use?
		while (true)
		{
			if (length <= 0x7F)
			{
				w.WriteByte((byte)length);
				return;
			}

			w.WriteByte((byte)((length & 0x7Fu) | 0x80u));
			length >>= 7;
		}
	}
	internal static void WriteUTF8EventWithLength(EndianBinaryWriter w, FLEvent ev, string str)
	{
		WriteArrayEventWithLength(w, ev, Encoding.UTF8.GetBytes(str));
	}
	internal static void WriteUTF16EventWithLength(EndianBinaryWriter w, FLEvent ev, string str)
	{
		WriteArrayEventWithLength(w, ev, Encoding.Unicode.GetBytes(str));
	}
	internal static void WriteArrayEventWithLength(EndianBinaryWriter w, FLEvent ev, ReadOnlySpan<byte> bytes)
	{
		w.WriteEnum(ev);
		WriteArrayEventLength(w, (uint)bytes.Length);
		w.WriteBytes(bytes);
	}

	private void WriteProjectInfo(EndianBinaryWriter w)
	{
		WriteVersion(w);
		WriteRegistration(w);

		Write32BitEvent(w, FLEvent.FineTempo, (uint)(CurrentTempo * 1_000));
		Write16BitEvent(w, FLEvent.SelectedPatternNum, 1);
		Write8BitEvent(w, FLEvent.IsSongMode, 1);
		Write8BitEvent(w, FLEvent.Shuffle, 0);
		Write16BitEvent(w, FLEvent.MasterPitch, 0);
		Write8BitEvent(w, FLEvent.ProjectTimeSigNumerator, TimeSigNumerator);
		Write8BitEvent(w, FLEvent.ProjectTimeSigDenominator, TimeSigDenominator);
		Write8BitEvent(w, FLEvent.ProjectShouldUseTimeSignatures, 1);
		Write8BitEvent(w, FLEvent.PanningLaw, 0); // Circular
		if (VersionCompat == FLVersionCompat.V21_0_3__B3517)
		{
			Write8BitEvent(w, FLEvent.PlaylistShouldUseAutoCrossfades, 0);
		}
		Write8BitEvent(w, FLEvent.ShouldPlayTruncatedClipNotes, 1);
		Write8BitEvent(w, FLEvent.ShouldShowInfoOnOpen, 0);
		WriteUTF16EventWithLength(w, FLEvent.ProjectTitle, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectGenre, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectAuthor, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectDataPath, "\0");
		WriteUTF16EventWithLength(w, FLEvent.ProjectComment, "\0");
		// ProjectURL would go here
		FLProjectTime.Write(w, DateTime.Now, TimeSpan.Zero);

		foreach (FLChannelFilter f in ChannelFilters)
		{
			f.Write(w);
		}
		Write32BitEvent(w, FLEvent.CurFilterNum, SelectedChannelFilter is null ? 0 : (uint)SelectedChannelFilter.Index);

		WriteArrayEventWithLength(w, FLEvent.CtrlRecChan, Array.Empty<byte>());
		// TODO: Why did this CtrlRecChan only show up sometimes? 0x1900 = 6_400
		// Bytes: CtrlRecChan - 12 = [0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00]
		foreach (FLPattern p in Patterns)
		{
			p.WritePatternNotes(w);
		}

		// No idea what these mean, but there are 3 and they're always the same in all my projects.
		// Dunno if it's MIDI keyboard related or something, because I only have 1
		WriteArrayEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo0);
		WriteArrayEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo1);
		WriteArrayEventWithLength(w, FLEvent.MIDIInfo, MIDIInfo2);
	}
	private void WriteVersion(EndianBinaryWriter w)
	{
		string v;
		uint b;

		switch (VersionCompat)
		{
			case FLVersionCompat.V20_9_2__B2963:
			{
				v = "20.9.2.2963";
				b = 2963;
				break;
			}
			case FLVersionCompat.V21_0_3__B3517:
			{
				v = "21.0.3.3517";
				b = 3517;
				break;
			}
			default: throw new InvalidOperationException("Invalid FL Version compatibility: " + VersionCompat);
		}

		WriteUTF8EventWithLength(w, FLEvent.Version, v + '\0');
		Write32BitEvent(w, FLEvent.VersionBuildNumber, b);
	}
	private static void WriteRegistration(EndianBinaryWriter w)
	{
		// When you save a project in FL Studio, the entire file becomes obfuscated if IsRegistered is 0 (trial mode).
		// Specifically, every 8bit/16bit/32bit event becomes obfuscated. The other array data is left alone (from the small glimpse I had), and it makes sense to leave it.
		// For example, the Unk_37 byte here became 76 instead of 1 in FL21. I didn't try different projects or FL versions, but if I had to guess, it is probably randomized and seeds the obfuscation.
		// Every other event also became obfuscated in some way that I couldn't quickly decipher with Windows calculator since it seems to mix the current position and eventID with the seed.
		// Examples:
		// ================
		// Byte: IsSongMode = 1 | IsSongMode = 62 (0x3E)
		// Byte: Shuffle = 0 | Shuffle = 61 (0x3D)
		// Byte: ProjectShouldUseTimeSignatures = 1 | ProjectShouldUseTimeSignatures = 53 (0x35)
		// Byte: PanningLaw = 0 | PanningLaw = 50 (0x32)
		// ================
		// Word: MasterPitch = 0 | MasterPitch = 15931 (0x3E3B)
		// Word: NewPattern = 1 | NewPattern = 8220 (0x201C)
		// Word: NewPattern = 2 | NewPattern = 6677 (0x1A15)
		// ================
		// DWord: FineTempo = 185000 | FineTempo = 1347393775 (0x504F98EF)
		// DWord: CurFilterNum = 0 | CurFilterNum = 757737252 (0x2D2A2724)

		// I can only imagine that trying to decipher this is trouble waiting to happen, so I won't try to.
		// They clearly want to obfuscate trial projects in a proprietary way, in order to prevent people from using FL in a trial and then using the project files with other software.
		// Reverse-engineering is protected by law in the USA (where I live), but Image-Line can make it against their TOS to try to decipher trial mode projects. They probably have, but I didn't check.
		// If you ever manage to reverse-engineer their method, they will 100% just change it to a new one, then you can't support new versions lol

		// They probably don't obfuscate the registered projects since it'd defeat the purpose of buying FL if you were trying to do something outside of it (like this).
		// If paying wouldn't make the project easier to parse, and you could defeat the obfuscation algorithm, you'd remain on the trial version or pirate it which is against Image-Line's interests.

		// Anyway, FL seems to be fine when reading projects that are in trial mode, even if they have no obfuscation. I assume this is due to the Unk_37 byte here still being 1 instead of 76.
		// So I am writing unregistered projects with no obfuscation. Thanks Image-Line for making this possible :)

		// I put my registration ID there as a reference to what it might look like.
		// I assume this is nothing to worry about since that would be so easy to find if you open a project I share. I've also seen other parsing projects show theirs.

		// Hopefully you enjoyed reading this part

		Write8BitEvent(w, FLEvent.IsRegistered, 0); // 1 if registered
		Write8BitEvent(w, FLEvent.Unk_37, 1); // Obfuscation-related?
		WriteUTF16EventWithLength(w, FLEvent.RegistrationID, "\0"); // Mine is "d3@?4xufs49p1n?B>;?889\0" in FL20 and FL21
	}
	private void WriteChannels(EndianBinaryWriter w)
	{
		foreach (FLAutomation a in Automations)
		{
			a.WriteAutomationConnection(w);
		}

		foreach (FLChannel c in Channels)
		{
			c.Write(w, VersionCompat);
		}
		// For some reason, pattern colors go between
		foreach (FLPattern p in Patterns)
		{
			p.WriteColorAndNameIfNecessary(w);
		}
		//
		foreach (FLAutomation a in Automations)
		{
			a.Write(w, VersionCompat, PPQN);
		}
	}
	private void WriteMoreStuffIDK(EndianBinaryWriter w)
	{
		Write16BitEvent(w, FLEvent.CurArrangementNum, SelectedArrangement is null ? (ushort)0 : SelectedArrangement.Index);
		Write8BitEvent(w, FLEvent.APDC, 1);
		Write8BitEvent(w, FLEvent.Unk_39, 1);
		Write8BitEvent(w, FLEvent.ShouldCutNotesFast, 0);
		Write8BitEvent(w, FLEvent.EEAutoMode, 0);
		Write8BitEvent(w, FLEvent.Unk_38, 1);
	}
	private void WriteMixer(EndianBinaryWriter w)
	{
		foreach (FLInsert i in Inserts)
		{
			i.Write(w, VersionCompat);
		}

		FLMixerParams.Write(w);

		Write32BitEvent(w, FLEvent.WindowH, 1286); // TODO: WindowH for what? Piano roll? Mixer?
	}
}
