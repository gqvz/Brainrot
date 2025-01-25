using System.Diagnostics;
using ConsoleAppFramework;

namespace Brainrot.TTS;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		await ConsoleApp.RunAsync(args, GenerateTTS);
	}

	/// <summary>
	/// GenerateTTS
	/// </summary>
	/// <param name="text">-t, Text to convert </param>
	/// <param name="file">-f, File to convert </param>
	/// <param name="outfile">-o, Output file </param>
	/// <param name="voice">-v, Voice to use </param>
	/// <param name="play">-p, Play the sound or not </param>
	private static async Task GenerateTTS(string text = "", string file = "", string outfile = "out.mp3", Voices voice = Voices.UsFemale1, bool play = false)
	{
		switch (text.Length)
		{
			case 0 when file.Length == 0:
				Console.WriteLine("Either text or file is required");
				return;
			case > 0 when file.Length > 0:
				Console.WriteLine("Only either text or file is allowed");
				return;
		}

		if (file.Length > 0)
			text = await File.ReadAllTextAsync(file);

		var downloader = new SpeechDownloader();

		var bytes = await downloader.Download(text, voice);

		await File.WriteAllBytesAsync(outfile, bytes);
		
		Console.WriteLine($"Audio saved to {outfile}");
		
		// fuck windows
		if (play) Process.Start(new ProcessStartInfo
		{
			FileName = "xdg-open",
			Arguments = outfile,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		});
	}
}