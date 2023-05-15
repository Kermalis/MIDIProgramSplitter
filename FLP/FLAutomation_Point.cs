using Kermalis.EndianBinaryIO;

namespace FLP;

partial class FLAutomation
{
	public struct Point
	{
		internal const int LEN = 24;

		public uint AbsoluteTicks;
		public double Value;

		internal void Write(EndianBinaryWriter w, uint ppqn, bool isFirst, bool isLast, uint nextPointAbsoluteTicks)
		{
			w.WriteDouble(Value);
			w.WriteSingle(0f); // Tension
			w.WriteUInt32(isFirst ? 0u : 2); // Hold for non-first ones

			// Delta ticks in quarter bars
			if (isLast)
			{
				w.WriteUInt64(0xFFFFFFFF00000001); // Special NaN for some reason
			}
			else
			{
				// Do Delta ticks for the previous point here.
				uint deltaTicks = nextPointAbsoluteTicks - AbsoluteTicks;
				w.WriteDouble(deltaTicks / (double)ppqn);
			}
		}
	}
}
