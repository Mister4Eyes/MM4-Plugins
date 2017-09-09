using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using MemeMachine4.PluginBase;

using Mister4Eyes.ArgParser;
using Mister4Eyes.GeneralUtilities;

using Youtube_Player.src;


public class YoutubePlayer : Plugin
{
	const string pattern = @"(?:(?:youtu\.be\/)|(?:watch\?v=))([\w\-]{11})";
	string YoutubeDl = string.Empty;
	YoutubeDownloader ytd;
	bool running = true;
	Task dLoop;
	Dictionary<string, string> CachedVideos = new Dictionary<string, string>();
	ConcurrentQueue<Tuple<string, SocketMessage>> downloadQueue = new ConcurrentQueue<Tuple<string, SocketMessage>>();

	~YoutubePlayer()
	{
		running = false;
		Console.WriteLine("Waiting for loop to shut down.");
		dLoop.Wait();
	}
	public YoutubePlayer(PluginArgs pag) : base(pag)
	{
		if(pag.section != null)
		{
			if (pag.section.HasKey("youtubeDL"))
			{
				YoutubeDl = pag.section.GetValue("youtubeDL");
				dLoop = Task.Run(DownloadLoop);
				
			}

			if (!File.Exists(YoutubeDl))
			{
				string searchFile = Utilities.FindFile("youtube-dl.exe");

				if (searchFile == null)
				{
					Console.WriteLine("Could not find youtube.dl.exe");
					YoutubeDl = null;
				}
				else
				{
					YoutubeDl = searchFile;
					dLoop = Task.Run(DownloadLoop);
					
				}
			}
		}
		else
		{
			string searchFile = Utilities.FindFile("youtube-dl.exe");

			if (searchFile == null)
			{
				Console.WriteLine("Could not find youtube.dl.exe");
				YoutubeDl = null;
			}
			else
			{
				YoutubeDl = searchFile;
				dLoop = Task.Run(DownloadLoop);
				
			}
		}

		ytd = new YoutubeDownloader(YoutubeDl, WorkingDirectory);

		Name = "Youtube Player";
		UsedFunctions = Functions.MessageReceived;
	}

	private async Task DownloadLoop()
	{
		while (running)
		{
			if (!downloadQueue.IsEmpty)
			{
				Tuple<string, SocketMessage> data;
				if(downloadQueue.TryDequeue(out data))
				{
					string vID = data.Item1;
					SocketMessage message = data.Item2;
					IVoiceChannel voiceChannel = ((IGuildUser)message.Author).VoiceChannel;

					if (CachedVideos.ContainsKey(vID))
					{
						await SendAudioFile(voiceChannel, CachedVideos[vID]);
						return;
					}

					string messageFormat = "Downloading Video...``{0}``\nElapsed: {1}";
					RestUserMessage updateMessage = await message.Channel.SendMessageAsync(string.Format(messageFormat, "|", new TimeSpan(0)));

					char[] spinner = { '|', '/', '-', '\\' };
					FileInfo ytFile = await ytd.DownloadVideo(vID, (seconds) =>
					{
						TimeSpan ts = new TimeSpan(seconds/3600, (seconds/60)%60, seconds%60);
						updateMessage.ModifyAsync((changes) =>
						{
							changes.Content = string.Format(messageFormat, spinner[seconds % spinner.Length], ts);
						});
					});

					if (ytFile == null)
					{
						await message.Channel.SendMessageAsync("Failure in getting the file.");
					}
					else
					{
						string yFile = ytFile.FullName;
						CachedVideos.Add(vID, yFile);

						await message.DeleteAsync();
						await SendAudioFile(voiceChannel, yFile);
					}
				}
			}

			await Task.Delay(100);
		}
	}
	public async override Task MessageReceived(SocketMessage message)
	{
		if(YoutubeDl != null)
		{
			string[] args = ArgumentParser.ParseArgumentString(message.Content);

			if (args.Length == 0)
			{
				return;
			}

			if(args[0] == "-stop")
			{
				IVoiceChannel voiceChannel = ((IGuildUser)message.Author).VoiceChannel;
				if (voiceChannel == null)
				{
					await message.Channel.SendMessageAsync("You must be connected to a voice channel!");
					return;
				}
				await StopCurrentAudio(voiceChannel);
				await message.Channel.SendMessageAsync("Stop request sent.");
			}
			else if(args[0] == "-play" && args.Length > 1)
			{
				if (((IGuildUser)message.Author).VoiceChannel == null)
				{
					await message.Channel.SendMessageAsync("You must be connected to a voice channel!");
					return;
				}

				Match match = Regex.Match(args[1], pattern);
				if (!match.Success)
				{
					await message.Channel.SendMessageAsync("Invalid youtube link.");
				}
				
				downloadQueue.Enqueue(new Tuple<string, SocketMessage>(match.Groups[1].Value, message));
			}
		}
	}
}