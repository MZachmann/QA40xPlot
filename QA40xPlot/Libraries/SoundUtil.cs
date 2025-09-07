

using NAudio.Wave;
using System.IO;


public class SoundUtil
{
	public static readonly int EchoNone = 0;
	public static readonly int EchoQuiet = 1;
	public static readonly int EchoBoth = 2;

	// play a sound from memory data to emulate sending a signal
	// the WaveOut it returns is still playing in a thread
	public static WaveOut PlaySound(double[] leftOut, double[] rightOut, int sampleRate)
	{
		var maxSound = Math.Max(leftOut.Max(), 8.0);
		Int16[] intData = leftOut.Select(s => (Int16)(s * Int16.MaxValue / maxSound)).ToArray();
		Int16[] intRData = rightOut.Select(s => (Int16)(s * Int16.MaxValue / maxSound)).ToArray();
		Int16[] stereoData = new Int16[intData.Length * 2];
		for (int i = 0; i < intData.Length; i++)
		{
			stereoData[i * 2] = intData[i];
			stereoData[i * 2 + 1] = intRData[i];
		}
		byte[] byteData = new byte[stereoData.Length * 2];
		Buffer.BlockCopy(stereoData, 0, byteData, 0, byteData.Length);

		IWaveProvider provider = new RawSourceWaveStream(
								 new MemoryStream(byteData), new WaveFormat(sampleRate, 16, 2));
		WaveOut waveOut = new();
		waveOut.Init(provider);
		waveOut.Play();
		return waveOut;
	}
}
