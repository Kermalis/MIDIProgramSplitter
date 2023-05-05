using Kermalis.EndianBinaryIO;

namespace FLP;

public sealed class FLInsert
{
	public sealed class FLFruityLSDOptions
	{
		public byte MIDIBank;
		public string DLSPath;
		public uint Icon;
		public FLColor3 Color;

		public FLFruityLSDOptions()
		{
			DLSPath = string.Empty;
			Color = new FLColor3(0x565148); // R 72, G 81, B 86
		}

		internal void Write(EndianBinaryWriter w, byte insertIndex)
		{
			FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.DefPluginName, "Fruity LSD\0");
			FLNewPlugin.WriteFruityLSD(w, insertIndex);
			FLProjectWriter.Write32BitEvent(w, FLEvent.PluginIcon, Icon);
			FLProjectWriter.Write32BitEvent(w, FLEvent.PluginColor, Color.GetFLValue());
			FLPluginParams.WriteFruityLSD(w, MIDIBank, DLSPath);
		}
	}

	private readonly byte _index;
	public FLFruityLSDOptions? FruityLSD;
	public FLColor3? Color;
	public ushort? Icon;
	public string? Name;

	internal FLInsert(byte index)
	{
		_index = index;
	}

	internal void Write(EndianBinaryWriter w)
	{
		// These 3 can exist independently of each other unlike patterns
		if (Color is not null)
		{
			FLProjectWriter.Write32BitEvent(w, FLEvent.InsertColor, Color.Value.GetFLValue());
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

		FruityLSD?.Write(w, _index);

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
