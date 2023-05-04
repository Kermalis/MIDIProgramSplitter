namespace FLP;

internal static partial class Utils
{
	/// <summary>Maps a value in the range [a1, a2] to [b1, b2]. Divide by zero occurs if a1 and a2 are equal</summary>
	public static float LerpUnclamped(float a1, float a2, float b1, float b2, float value)
	{
		return b1 + ((value - a1) / (a2 - a1) * (b2 - b1));
	}
	/// <inheritdoc cref="LerpUnclamped(float, float, float, float, float)"/>
	public static double LerpUnclamped(double a1, double a2, double b1, double b2, double value)
	{
		return b1 + ((value - a1) / (a2 - a1) * (b2 - b1));
	}
	/// <inheritdoc cref="LerpUnclamped(float, float, float, float, float)"/>
	public static decimal LerpUnclamped(decimal a1, decimal a2, decimal b1, decimal b2, decimal value)
	{
		return b1 + ((value - a1) / (a2 - a1) * (b2 - b1));
	}
}