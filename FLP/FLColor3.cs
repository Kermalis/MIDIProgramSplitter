using System;

namespace FLP;

public struct FLColor3
{
	public byte R;
	public byte G;
	public byte B;

	/// <summary>This constructor takes <see cref="R"/> from the LSB and <see cref="B"/> from the MSB</summary>
	public FLColor3(uint flValue)
	{
		R = (byte)(flValue & 0xFF);
		G = (byte)((flValue >> 8) & 0xFF);
		B = (byte)((flValue >> 16) & 0xFF);
	}
	public FLColor3(byte r, byte g, byte b)
	{
		R = r;
		G = g;
		B = b;
	}

	public static FLColor3 GetRandom()
	{
		return new FLColor3((uint)Random.Shared.Next(0x1_000_000));
	}
	public static FLColor3 FromRGB(uint rgb)
	{
		byte b = (byte)(rgb & 0xFF);
		byte g = (byte)((rgb >> 8) & 0xFF);
		byte r = (byte)((rgb >> 16) & 0xFF);
		return new FLColor3(r, g, b);
	}

	public uint GetFLValue()
	{
		return R | ((uint)G << 8) | ((uint)B << 16);
	}

	public override string ToString()
	{
		return string.Format("R {0} G {1} B {2}", R, G, B);
	}
}
