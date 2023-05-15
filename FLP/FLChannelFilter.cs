using Kermalis.EndianBinaryIO;

namespace FLP;

public sealed class FLChannelFilter
{
	internal ushort Index;

	public string Name;

	internal FLChannelFilter(string name)
	{
		Name = name;
	}

	internal void Write(EndianBinaryWriter w)
	{
		FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.ChanFilterName, Name + '\0');
	}
}
