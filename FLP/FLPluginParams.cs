using Kermalis.EndianBinaryIO;
using Kermalis.MIDI;
using System.Text;

namespace FLP;

internal struct FLPluginParams
{
	public static void WriteMIDIOut(EndianBinaryWriter w, byte midiChannel, byte midiBank, MIDIProgram program)
	{
		w.WriteEnum(FLEvent.PluginParams);
		FLProjectWriter.WriteArrayEventLength(w, 383);

		w.WriteUInt32(6);
		w.WriteUInt32(midiChannel);
		w.WriteInt32(-1);
		w.WriteInt32(-1);
		w.WriteInt32(-1);
		w.WriteInt32(0);
		w.WriteInt32(midiBank);

		w.WriteUInt16(1);
		w.WriteUInt16((byte)(program + 1));

		w.WriteZeroes(290);

		w.WriteSByte(-1);
		for (int i = 1; i <= 8; i++)
		{
			string s = "Page " + i;
			w.WriteByte((byte)s.Length);
			w.WriteChars(s);
		}

		w.WriteUInt32(0);
	}

	public static void WriteFruityLSD(EndianBinaryWriter w, byte bankID, string dlsPath)
	{
		byte[] pathBytes = Encoding.UTF8.GetBytes(dlsPath);
		byte dlsPathLen = (byte)pathBytes.Length;

		w.WriteEnum(FLEvent.PluginParams);
		FLProjectWriter.WriteArrayEventLength(w, (uint)(97 + dlsPathLen));

		w.WriteUInt32(0);
		w.WriteUInt32(0x80); // 128
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(bankID);

		w.WriteByte(dlsPathLen); // It's possible this is a varLen length, but I didn't check
		w.WriteBytes(pathBytes);

		w.WriteZeroes(7);

		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0x80); // 128
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteByte(0);
	}
}
