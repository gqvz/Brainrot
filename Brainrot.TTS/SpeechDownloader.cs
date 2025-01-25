using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Brainrot.TTS;

// The logic is taken from https://github.com/mark-rez/TikTok-Voice-TTS
public sealed partial class SpeechDownloader
{
	// I don't know if this violates the tos or not
	private static readonly Uri ApiUri = new("https://gesserit.co/api/tiktok-tts");

	private readonly HttpClient _httpClient = new();

	public async Task<byte[]> Download(string text, Voices voice)
	{
		var splits = SplitText(text);
		var audioChunks = new string[splits.Count];

		var tasks = splits.Select((split, index) => SaveAudioChunk(split, index, voice)).ToList();

		await Task.WhenAll(tasks);
		
		return audioChunks.Select(Convert.FromBase64String).SelectMany(chunk => chunk).ToArray();

		async Task SaveAudioChunk(string textChunk, int index, Voices voice1)
		{
			var base64 = await GenerateAudioChunk(textChunk, voice1);
			audioChunks[index] = base64;
		}
	}

	private async Task<string> GenerateAudioChunk(string textChunk, Voices voice)
	{
		var response = await _httpClient.PostAsync(ApiUri, JsonContent.Create(new
		{
			text = textChunk,
			voice = voice.ToVoiceId()
		}));
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadFromJsonAsync<Response>() ?? throw new NullReferenceException();
		return json.Base64;
	}

	[GeneratedRegex(@".*?[.,!?:;-]|.+")]
	private static partial Regex PunctuationRegex();

	[GeneratedRegex(@".*?[ ]|.+")]
	private static partial Regex SpaceRegex();

	private static List<string> SplitText(string text, int characterLimit = 300)
	{
		var mergedChunks = new List<string>();
		var separatedChunks = PunctuationRegex().Matches(text);
		var chunkList = new List<string>();

		foreach (Match match in separatedChunks)
		{
			chunkList.Add(match.Value);
		}

		// Further split any chunks longer than the character limit
		for (var i = 0; i < chunkList.Count; i++)
		{
			var chunk = chunkList[i];
			if (Encoding.UTF8.GetByteCount(chunk) > characterLimit)
			{
				var subChunks = SpaceRegex().Matches(chunk);
				var subChunkList = new List<string>();
				foreach (Match match in subChunks)
				{
					subChunkList.Add(match.Value);
				}
				chunkList.RemoveAt(i);
				chunkList.InsertRange(i, subChunkList);
				i += subChunkList.Count - 1; // Adjust index
			}
		}

		// Combine chunks into segments of characterLimit or less
		var currentChunk = string.Empty;

		foreach (var chunk in chunkList)
		{
			if (Encoding.UTF8.GetByteCount(currentChunk) + Encoding.UTF8.GetByteCount(chunk) <= characterLimit)
			{
				currentChunk += chunk;
			}
			else
			{
				mergedChunks.Add(currentChunk);
				currentChunk = chunk;
			}
		}

		// Append the last chunk
		if (!string.IsNullOrEmpty(currentChunk))
		{
			mergedChunks.Add(currentChunk);
		}

		return mergedChunks;
	}

	// ReSharper disable once ClassNeverInstantiated.Local
	private record Response([property: JsonPropertyName("base64")] string Base64);
}