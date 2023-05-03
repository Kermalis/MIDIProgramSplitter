#if DEBUG && WINDOWS
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MIDIProgramSplitter.CMD;

internal static partial class WinUtils
{
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
}
#endif