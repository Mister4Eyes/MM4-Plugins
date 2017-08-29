using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using MemeMachine4.PluginBase;
using OverwatchStatistics.src;

using Mister4Eyes.ArgParser;
namespace OverwatchStatistics
{
	public class OwStats : Plugin
	{
		public OwStats(PluginArgs pag) : base(pag)
		{
			Name = "Overwatch Stats.";
			UsedFunctions = Functions.MessageReceived;
		}

		public async override Task MessageReceived(SocketMessage message)
		{
			string[] args = ArgumentParser.ParseArgumentString(message.Content);
			if(args.Length > 0)
			{
				if (args[0] == "-owStats")
				{
					if(args.Length > 1)
					{
						using (IDisposable typing = message.Channel.EnterTypingState())
						{
							Player player = new Player(args[1]);
							if (player.Exists)
							{
								await message.Channel.SendMessageAsync($"```{MainsToString(player.GetMains())}```");
							}
							else
							{
								await message.Channel.SendMessageAsync($"{args[1]} could not be found.");
							}
						}
					}
					else
					{
						await message.Channel.SendFileAsync("Not enough arguments.");
					}
				}
			}
		}

		static string MainsToString(Tuple<Hero[], Hero[]> mains)
		{
			StringBuilder sb = new StringBuilder();
			Action<Hero[]> AppendHeros = new Action<Hero[]>((heros) =>
			{
				//Writes in decending order.
				for (int i = heros.Length - 1; i >= 0; --i)
				{
					Hero hero = heros[i];
					sb.AppendLine($"{hero.Name}\tTime Spent:{hero.Hours}");
				}
			});

			if (mains.Item2 == null)
			{
				if (mains.Item1.Length == 0)
				{
					sb.AppendLine("No mains detected.");
				}
				else
				{
					string primary = "Main" + ((mains.Item1.Length > 1) ? "s\n" : ": ");
					sb.Append(primary);
					AppendHeros(mains.Item1);
				}
			}
			else
			{
				string primary = "Primary Main" + ((mains.Item2.Length > 1) ? "s\n" : ": ");
				string secondary = "\nSecondary Main" + ((mains.Item1.Length > 1) ? "s\n" : ": ");

				sb.Append(primary);
				AppendHeros(mains.Item2);

				sb.Append(secondary);
				AppendHeros(mains.Item1);

			}
			return sb.ToString();
		}
	}
}
