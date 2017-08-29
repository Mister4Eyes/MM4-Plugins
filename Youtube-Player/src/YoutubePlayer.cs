using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

using MemeMachine4.PluginBase;

using Mister4Eyes.ArgParser;
using Mister4Eyes.GeneralUtilities;

using Youtube_Player.src;

using Discord;
using System.Text.RegularExpressions;

public class YoutubePlayer : Plugin
{
	const string pattern = @"(?:(?:youtu\.be\/)|(?:watch\?v=))([\w\-]{11})";
	string YoutubeDl = string.Empty;
	YoutubeDownloader ytd;

	Dictionary<string, string> CachedVideos = new Dictionary<string, string>();
	public YoutubePlayer(PluginArgs pag) : base(pag)
	{
		if(pag.section != null)
		{
			if (pag.section.HasKey("youtubeDL"))
			{
				YoutubeDl = pag.section.GetValue("youtubeDL");
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
			}
		}

		ytd = new YoutubeDownloader(YoutubeDl, WorkingDirectory);

		Name = "Youtube Player";
		UsedFunctions = Functions.MessageReceived;
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
				IVoiceChannel voiceChannel = ((IGuildUser)message.Author).VoiceChannel;
				if (voiceChannel == null)
				{
					await message.Channel.SendMessageAsync("You must be connected to a voice channel!");
					return;
				}

				Match match = Regex.Match(args[1], pattern);
				if (!match.Success)
				{
					await message.Channel.SendMessageAsync("Invalid youtube link.");
				}

				string vID = match.Groups[1].Value;

				if (CachedVideos.ContainsKey(vID))
				{
					await SendAudioFile(voiceChannel, CachedVideos[vID]);
					return;
				}

				FileInfo ytFile = ytd.DownloadVideo(vID);

				if(ytFile == null)
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
	}
}