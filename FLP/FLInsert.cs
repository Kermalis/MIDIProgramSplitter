using Kermalis.EndianBinaryIO;
using System;

namespace FLP;

public sealed class FLInsert
{
	public sealed class FLFruityLSDOptions
	{
		public byte MIDIBank;
		public string DLSPath;
		public uint Icon;
		public FLColor3 Color;

		public FLFruityLSDOptions(byte midiBank, string dlsPath, FLColor3 color)
		{
			MIDIBank = midiBank;
			DLSPath = dlsPath;
			Color = color;
		}

		public static FLColor3 GetDefaultColor(FLVersionCompat verCom)
		{
			switch (verCom)
			{
				case FLVersionCompat.V20_9_2__B2963: return new FLColor3(72, 81, 86);
				case FLVersionCompat.V21_0_3__B3517: return new FLColor3(92, 101, 106);
			}
			throw new ArgumentOutOfRangeException(nameof(verCom), verCom, null);
		}

		internal void Write(EndianBinaryWriter w, FLVersionCompat verCom, byte insertIndex)
		{
			FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.DefPluginName, "Fruity LSD\0");
			FLNewPlugin.WriteFruityLSD(w, insertIndex);
			FLProjectWriter.Write32BitEvent(w, FLEvent.PluginIcon, Icon);
			FLProjectWriter.Write32BitEvent(w, FLEvent.PluginColor, Color.GetFLValue());
			if (verCom == FLVersionCompat.V21_0_3__B3517)
			{
				byte val = (byte)(Color.Equals(GetDefaultColor(verCom)) ? 0 : 1);
				FLProjectWriter.Write8BitEvent(w, FLEvent.PluginIgnoresTheme, val);
			}
			FLPluginParams.WriteFruityLSD(w, MIDIBank, DLSPath);
		}
	}

	public static FLColor3 DefaultColor => new(99, 108, 113);

	private readonly byte _index;
	public FLFruityLSDOptions? FruityLSD;
	public FLColor3 Color;
	public ushort? Icon;
	public string? Name;

	internal FLInsert(byte index)
	{
		_index = index;
		Color = DefaultColor;
	}

	internal void Write(EndianBinaryWriter w, FLVersionCompat verCom)
	{
		// Color/Icon/Name can exist independently of each other unlike patterns
		bool isDefaultColor = Color.Equals(DefaultColor);
		if (!isDefaultColor)
		{
			FLProjectWriter.Write32BitEvent(w, FLEvent.InsertColor, Color.GetFLValue());
		}
		// If color is present, it goes above this
		if (verCom == FLVersionCompat.V21_0_3__B3517)
		{
			byte val = (byte)(isDefaultColor ? 0 : 1);
			FLProjectWriter.Write8BitEvent(w, FLEvent.InsertIgnoresTheme, val);
		}
		if (Icon is not null)
		{
			FLProjectWriter.Write16BitEvent(w, FLEvent.InsertIcon, Icon.Value);
		}
		if (Name is not null)
		{
			FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.InsertName, Name + '\0');
		}

		bool isMasterOrCurrent = _index is 0 or 126;
		FLInsertParams.Write(w, isMasterOrCurrent);

		FruityLSD?.Write(w, verCom, _index);

		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 0);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 1);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 2);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 3);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 4);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 5);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 6);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 7);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 8);
		FLProjectWriter.Write16BitEvent(w, FLEvent.NewInsertSlot, 9);
		if (isMasterOrCurrent)
		{
			WriteRouting_None(w);
		}
		else
		{
			WriteRouting_GoToMaster(w);
		}
		FLProjectWriter.Write32BitEvent(w, FLEvent.Unk_165, 3);
		FLProjectWriter.Write32BitEvent(w, FLEvent.Unk_166, 1);
		FLProjectWriter.Write32BitEvent(w, FLEvent.InsertInChanNum, uint.MaxValue);
		FLProjectWriter.Write32BitEvent(w, FLEvent.InsertOutChanNum, _index == 0 ? 0 : uint.MaxValue);
	}
	private static void WriteRouting_None(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.InsertRouting);
		FLProjectWriter.WriteArrayEventLength(w, 127);

		// bool[127]. Go to nothing
		w.WriteZeroes(127);
	}
	private static void WriteRouting_GoToMaster(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.InsertRouting);
		FLProjectWriter.WriteArrayEventLength(w, 127);

		// bool[127]. Go to master and nothing else
		w.WriteByte(1);
		w.WriteZeroes(126);
	}
}
