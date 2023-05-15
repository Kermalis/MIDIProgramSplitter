using Kermalis.EndianBinaryIO;
using System;

namespace FLP;

internal struct FLProjectTime
{
	private static DateTime BaseDate => new(1899, 12, 30);

	public static string ReadData(byte[] bytes)
	{
		double startOffset = EndianBinaryPrimitives.ReadDouble(bytes, Endianness.LittleEndian);
		double daysWorked = EndianBinaryPrimitives.ReadDouble(bytes.AsSpan(8), Endianness.LittleEndian);
		return string.Format("{{ Created: {0}, TimeSpent: {1} }}", BaseDate.AddDays(startOffset), TimeSpan.FromDays(daysWorked));
	}

	public static void Write(EndianBinaryWriter w, DateTime creationDateTime, TimeSpan timeSpent)
	{
		w.WriteEnum(FLEvent.ProjectTime);
		FLProjectWriter.WriteArrayEventLength(w, 16);

		w.WriteDouble((creationDateTime - BaseDate).TotalDays);
		w.WriteDouble(timeSpent.TotalDays);
	}
}
