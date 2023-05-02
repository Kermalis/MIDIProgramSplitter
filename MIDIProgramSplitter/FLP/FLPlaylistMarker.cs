using Kermalis.EndianBinaryIO;

namespace MIDIProgramSplitter.FLP;

internal sealed class FLPlaylistMarker
{
	public uint AbsoluteTicks;
	/// <summary>Empty uses default name "Marker #1" for example</summary>
	public string Name;
	public (byte num, byte denom)? TimeSig;

	public FLPlaylistMarker(uint ticks, string name, (byte, byte)? timeSig)
	{
		AbsoluteTicks = ticks;
		Name = name;
		TimeSig = timeSig;
	}

	public void Write(EndianBinaryWriter w)
	{
		uint add = TimeSig is null ? 0u : 0x08_000_000;
		FLProjectWriter.WriteDWordEvent(w, FLEvent.NewTimeMarker, AbsoluteTicks + add);

		if (TimeSig is not null)
		{
			(byte num, byte denom) = TimeSig.Value;
			FLProjectWriter.WriteByteEvent(w, FLEvent.TimeSigMarkerNumerator, num);
			FLProjectWriter.WriteByteEvent(w, FLEvent.TimeSigMarkerDenominator, denom);
		}
		FLProjectWriter.WriteUTF16EventWithLength(w, FLEvent.TimeMarkerName, Name + '\0');
	}
}
