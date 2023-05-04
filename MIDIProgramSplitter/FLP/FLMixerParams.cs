using Kermalis.EndianBinaryIO;
using System.IO;
using System.Text;

namespace MIDIProgramSplitter.FLP;

internal static class FLMixerParams
{
	private const int BYTES_PER_EVENT = 0xC;
	// There are 4697 events:
	//             1 = Master Volume event
	// 100*38 [3800] = Insert 0-99 events (includes Unk_A4, Unk_A5, Unk_A6, Unk_A7, Unk_A8, Unk_BE)
	// 5*34    [170] = Insert 100-104 events (includes Unk_A8, Unk_BE)
	// 22*33   [726] = Insert 105-126 events (includes Unk_BE)
	private const int NUM_EVENTS =
		1
		+ (100 * 38)
		+ (5 * 34)
		+ (22 * 33);
	private const int LEN = NUM_EVENTS * BYTES_PER_EVENT;

	public static void Write(EndianBinaryWriter w)
	{
		w.WriteEnum(FLEvent.MixerParams);
		FLProjectWriter.WriteArrayEventLength(w, LEN);

		WriteMasterVolumeEvent(w, 0x3200);

		for (byte insertID = 0; insertID < 127; insertID++)
		{
			for (byte slotID = 0; slotID < 10; slotID++)
			{
				WriteInsertSlotEvent(w, FLMixerParamsEvent.SlotState, insertID, slotID, 1);
				WriteInsertSlotEvent(w, FLMixerParamsEvent.SlotVolume, insertID, slotID, 0x3200);
			}
			WriteInsertEvent(w, FLMixerParamsEvent.Volume, insertID, 0x3200);
			WriteInsertEvent(w, FLMixerParamsEvent.Pan, insertID, 0);
			WriteInsertEvent(w, FLMixerParamsEvent.StereoSeparation, insertID, 0);
			WriteInsertEvent(w, FLMixerParamsEvent.LowLevel, insertID, 0);
			WriteInsertEvent(w, FLMixerParamsEvent.BandLevel, insertID, 0);
			WriteInsertEvent(w, FLMixerParamsEvent.HighLevel, insertID, 0);
			WriteInsertEvent(w, FLMixerParamsEvent.LowFreq, insertID, 0x1691);
			WriteInsertEvent(w, FLMixerParamsEvent.BandFreq, insertID, 0x8179);
			WriteInsertEvent(w, FLMixerParamsEvent.HighFreq, insertID, 0xDA11);
			WriteInsertEvent(w, FLMixerParamsEvent.LowWidth, insertID, 0x445C);
			WriteInsertEvent(w, FLMixerParamsEvent.BandWidth, insertID, 0x445C);
			WriteInsertEvent(w, FLMixerParamsEvent.HighWidth, insertID, 0x445C);

			if (insertID < 100)
			{
				WriteInsertEvent(w, FLMixerParamsEvent.Unk_A4, insertID, 0);
				WriteInsertEvent(w, FLMixerParamsEvent.Unk_A5, insertID, 0);
				WriteInsertEvent(w, FLMixerParamsEvent.Unk_A6, insertID, 0);
				WriteInsertEvent(w, FLMixerParamsEvent.Unk_A7, insertID, 0);
			}
			if (insertID < 105)
			{
				WriteInsertEvent(w, FLMixerParamsEvent.Unk_A8, insertID, 0);
			}
			WriteInsertEvent(w, FLMixerParamsEvent.Unk_BE, insertID, 0);
		}
	}
	private static void WriteMasterVolumeEvent(EndianBinaryWriter w, uint vol)
	{
		WriteEvent(w, 0, 2, 0, 0, vol);
	}
	private static void WriteInsertSlotEvent(EndianBinaryWriter w, FLMixerParamsEvent eType, byte insertID, byte slotID, uint eData)
	{
		WriteEvent(w, eType, 1, insertID, slotID, eData);
	}
	private static void WriteInsertEvent(EndianBinaryWriter w, FLMixerParamsEvent eType, byte insertID, uint eData)
	{
		WriteEvent(w, eType, 1, insertID, 0, eData);
	}
	private static void WriteEvent(EndianBinaryWriter w, FLMixerParamsEvent eType, byte insertType, byte insertID, byte slotID, uint eData)
	{
		w.WriteUInt32(0);
		w.WriteEnum(eType);
		w.WriteByte(0x1F);
		w.WriteUInt16((ushort)(slotID | (insertID << 6) | (insertType << 13)));
		w.WriteUInt32(eData);
	}

	public static string ReadData(byte[] bytes)
	{
		if (bytes.Length != LEN)
		{
			throw new InvalidDataException("Unexpected MixerParams length: " + bytes.Length);
		}

		using (var ms = new MemoryStream(bytes))
		{
			var r = new EndianBinaryReader(ms);
			var str = new StringBuilder();

			str.AppendLine("{");

			while (ms.Position < ms.Length)
			{
				long startPos = ms.Position;

				uint unk0 = r.ReadUInt32(); // Always 0?
				var eType = (FLMixerParamsEvent)r.ReadByte();
				byte unk5 = r.ReadByte(); // Always 0x1F (31) [0b0001_1111]?
				ushort eFlags = r.ReadUInt16();
				uint eData = r.ReadUInt32();

				// eFlags bits: [ttti iiii iiss ssss]
				int slotId = eFlags & 0x3F; // s: [0, 63]
				int insertId = (eFlags >> 6) & 0x7F; // i: [0, 127]
				int insertType = eFlags >> 13; // t: [0, 7]

				str.Append($"t {insertType} @ 0x{startPos:X4} => ");

				if (insertType == 2)
				{
					str.Append($"Master Volume = 0x{eData:X}");
				}
				else if (insertType == 1)
				{
					str.Append($"Insert #{insertId} slot #{slotId} {eType} = 0x{eData:X}");
				}
				else
				{
					str.Append($"Unknown: i={insertId} s={slotId} eType={eType}, eData=0x{eData:X8}");
				}

				str.AppendLine($" (unk0=0x{unk0}, unk5=0x{unk5:X})");
			}

			str.Append('}');
			return str.ToString();
		}
	}
}
