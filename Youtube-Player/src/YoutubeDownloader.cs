using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Mister4Eyes.GeneralUtilities;

namespace Youtube_Player.src
{
	class YoutubeDownloader
	{
		const string pattern = @"([\w\-]{11})";
		string YtdlLoc;
		string OutDirectory;

		public YoutubeDownloader(string file, string outpDir)
		{
			if (file == null)
			{
				YtdlLoc = Utilities.FindFile("youtube-dl.exe");
			}
			else
			{
				FileInfo fi = new FileInfo(file);
				if (fi.Exists && fi.Extension == ".exe")
				{
					YtdlLoc = file;
				}
				else
				{
					YtdlLoc = null;
				}

			}

			if (!Directory.Exists(outpDir))
			{
				Directory.CreateDirectory(outpDir);
			}

			OutDirectory = outpDir;
		}


		public async Task<FileInfo> DownloadVideo(string url, Action<int> updateFunction)
		{
			Match match = Regex.Match(url, pattern);
			if (match.Success)
			{
				string videoId = match.Groups[1].Value;
				ProcessStartInfo start = new ProcessStartInfo();
				//Uses this instead of the url because even if the url is malformed, the data can still get through.
				DirectoryInfo di = new DirectoryInfo($@"{OutDirectory}\Temp\");
				if (!di.Exists)
				{
					di = Directory.CreateDirectory(di.FullName);
				}


				//We look for the video we just downloaded.
				//We have to look for it because we don't know how the file was outputted.
				Func<FileInfo> ContainsVid = new Func<FileInfo>(() =>
				{
					foreach (FileInfo fi in di.EnumerateFiles())
					{
						if (fi.Name.StartsWith(videoId))
						{
							return fi;
						}
					}
					return null;
				});

				FileInfo rFi;

				if ((rFi = ContainsVid()) != null)
				{
					return rFi;
				}

				start.Arguments = $@"https://youtu.be/{videoId} -o ""{OutDirectory}\Temp\{videoId}.%(ext)s""";
				start.FileName = YtdlLoc;
				start.UseShellExecute = false;

				Stopwatch sw = new Stopwatch();
				Process process = Process.Start(start);
				sw.Start();

				while (!process.HasExited && sw.Elapsed.TotalMinutes < 10)
				{
					process.Refresh();
					updateFunction((int)Math.Round(sw.Elapsed.TotalSeconds));
					await Task.Delay(500);
				}

				sw.Stop();

				if (sw.Elapsed.TotalMinutes >= 10)
				{
					process.Kill();
					Console.WriteLine("Process took too long to execute.");
					return null;
				}
				return ContainsVid();
			}

			return null;
		}
	}
}
