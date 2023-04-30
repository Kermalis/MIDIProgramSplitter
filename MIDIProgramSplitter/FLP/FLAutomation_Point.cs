using Kermalis.EndianBinaryIO;

namespace MIDIProgramSplitter.FLP;

partial class FLAutomation
{
	public struct Point
	{
		public const int LEN = 24;

		public uint AbsoluteTicks;
		public double Value;

		public void Write(EndianBinaryWriter w, uint ppqn, bool isFirst, bool isLast, uint nextPointAbsoluteTicks)
		{
			w.WriteDouble(Value);
			w.WriteSingle(0f); // Tension
			w.WriteUInt32(isFirst ? 0u : 2); // Hold for non-first ones

			// Delta ticks in quarter bars
			if (isLast)
			{
				//w.WriteDouble(double.NaN); // TODO: Use special nan FL uses?
				w.WriteUInt64(0xFFFFFFFF00000001);
			}
			else
			{
				// Do Delta ticks for the previous point here.
				uint deltaTicks = nextPointAbsoluteTicks - AbsoluteTicks;
				double d = deltaTicks / ppqn;
				d *= 4;
				w.WriteDouble(d); // TODO: Check
			}
		}
	}
}
