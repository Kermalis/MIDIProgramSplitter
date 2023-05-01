using System;
#if DEBUG && WINDOWS
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace MIDIProgramSplitter;

internal static partial class Utils
{
#if DEBUG && WINDOWS
	private const int CF_UNICODETEXT = 13;

	[LibraryImport("user32.dll")]
	private static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool OpenClipboard(IntPtr hWndNewOwner);
	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool EmptyClipboard();
	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool CloseClipboard();

	public static void Win_SetClipboardString(string s)
	{
		string nullTerminatedStr = s + '\0';
		byte[] strBytes = Encoding.Unicode.GetBytes(nullTerminatedStr);
		IntPtr hglobal = Marshal.AllocHGlobal(strBytes.Length);
		Marshal.Copy(strBytes, 0, hglobal, strBytes.Length);
		OpenClipboard(IntPtr.Zero);
		EmptyClipboard();
		SetClipboardData(CF_UNICODETEXT, hglobal);
		CloseClipboard();
		Marshal.FreeHGlobal(hglobal);
	}
#endif

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