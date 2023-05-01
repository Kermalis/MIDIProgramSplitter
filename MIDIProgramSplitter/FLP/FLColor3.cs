namespace MIDIProgramSplitter.FLP;

internal struct FLColor3
{
	public byte R;
	public byte G;
	public byte B;

	public FLColor3(uint value)
	{
		R = (byte)(value & 0xFF);
		G = (byte)((value >> 8) & 0xFF);
		B = (byte)((value >> 16) & 0xFF);
		// No A
	}

	public uint GetValue()
	{
		return R | ((uint)G << 8) | ((uint)B << 16);
	}
}
