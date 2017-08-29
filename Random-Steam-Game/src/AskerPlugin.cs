using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord.WebSocket;

using MemeMachine4.PluginBase;

using Mister4Eyes.ArgParser;

namespace Random_Steam_Game
{
	struct SteamDiscordId
	{
		public ulong DiscordId;
		public ulong SteamId;
		public SteamDiscordId(ulong discordId, ulong steamId)
		{
			DiscordId = discordId;
			SteamId = steamId;
		}

		public void Save(FileStream stream)
		{
			stream.Write(BitConverter.GetBytes(DiscordId),	0, sizeof(ulong));
			stream.Write(BitConverter.GetBytes(SteamId),	0, sizeof(ulong));
		}
	}

	public class Random_Steam_Game : Plugin
	{
		const string fileLoc = @".\userData.dat";
		bool running = true;
		Task SaveL;
		SteamClient Steam = null;
		Dictionary<ulong, ulong> Links = new Dictionary<ulong, ulong>();
		Random r = new Random();

		//Nothing about this is syncronous.
		//It's just to save data
		~Random_Steam_Game()
		{
			Save();
			Console.WriteLine("Saved data.");
		}

		private Task SaveLoop()
		{
			int pMinute = DateTime.Now.Minute;
			while(running)
			{
				int cMinute = DateTime.Now.Minute;

				if(cMinute != pMinute && cMinute < pMinute)
				{
					Save();
					pMinute = cMinute;
				}
			}

			return Task.CompletedTask;
		}

		private void Save()
		{
			Console.WriteLine("Saving data.");
			if(Links.Count == 0)
			{
				return;
			}
			using (FileStream stream = SaveFileStream(fileLoc))
			{
				foreach(KeyValuePair<ulong, ulong> keyValue in Links.AsQueryable())
				{
					stream.Write(BitConverter.GetBytes(keyValue.Key),	0, sizeof(ulong));
					stream.Write(BitConverter.GetBytes(keyValue.Value),	0, sizeof(ulong));
				}
			}
		}

		private void Load()
		{
			Console.WriteLine("Loading data.");
			using (FileStream stream = LoadFileStream(fileLoc))
			{
				if(stream != null)
				{
					byte[] buffer = new byte[sizeof(ulong) * 2];
					int count;
					while ((count = stream.Read(buffer, 0, sizeof(ulong)*2)) == sizeof(ulong)*2)
					{
						ulong DiscordId = BitConverter.ToUInt64(buffer, 0);
						ulong SteamId	= BitConverter.ToUInt64(buffer, sizeof(ulong));
						Links.Add(DiscordId, SteamId);
					}
				}
			}
		}

		private async Task<SteamGame[]> SuToGame(SocketUser su)
		{
			if (Links.ContainsKey(su.Id))
			{
				return await Task.Run(() =>
				{
					return Steam.GetGames(Links[su.Id], true, true);
				});
			}
			else
			{
				return null;
			}
		}

		private async Task PickGame(SocketMessage message, bool randomPick = true)
		{
			{
				SteamGame[] Games = await SuToGame(message.Author);
				if (Games == null)
				{
					await message.Channel.SendMessageAsync($"{message.Author.Mention} has not registered their steam account.");
				}
				else
				{
					List<SteamGame> AvailableGames = new List<SteamGame>(Games);

					//First pass is used to cull out single player games.
					//Because testing if something is multiplayer takes time,
					//Knowing that all the games that are in there have multiplayer means that we don't need to test if it's multiplayer.
					//Thus that is saving time.
					using (IDisposable typing = message.Channel.EnterTypingState())
					{
						bool FirstPass = true;
						foreach (SocketUser Su in message.MentionedUsers)
						{
							List<SteamGame> CurrentAvailGames = new List<SteamGame>();
							Games = await SuToGame(Su);
							if (Games == null)
							{
								await message.Channel.SendMessageAsync($"{Su.Mention} has not registered their steam account.");
							}
							else
							{
								//Goes through both in order to find a match.
								foreach (SteamGame cGame in Games)
								{
									foreach (SteamGame aGame in AvailableGames)
									{
										if (cGame.appid == aGame.appid && (!FirstPass || aGame.HasMultiplayer))
										{
											CurrentAvailGames.Add(cGame);
											break;
										}
									}
								}
							}
							FirstPass = false;
							AvailableGames = CurrentAvailGames;
						}

						if (randomPick)
						{
							if(AvailableGames.Count == 0)
							{
								await message.Channel.SendMessageAsync("No games in common.");
							}
							else
							{
								SteamGame rGame = AvailableGames[r.Next(AvailableGames.Count)];
								await message.Channel.SendMessageAsync(rGame.name);
							}
						}
						else
						{
							StringBuilder sbs = new StringBuilder();
							bool sent = false;
							foreach (SteamGame game in AvailableGames)
							{
								StringBuilder sb = new StringBuilder();
								sb.AppendLine($"{game.name}");

								if (sb.Length + sbs.Length > 2000)
								{
									sent = true;
									await message.Channel.SendMessageAsync(sbs.ToString());
									sbs.Clear();
								}
								sbs.Append(sb.ToString());
							}
							if (sbs.Length == 0 && !sent)
							{
								await message.Channel.SendMessageAsync("No games in common.");
							}
							else if (sbs.Length != 0)
							{
								await message.Channel.SendMessageAsync(sbs.ToString());
							}
						}
					}
				}
			}
		}
		public async override Task MessageReceived(SocketMessage message)
		{
			string[] args = ArgumentParser.ParseArgumentString(message.Content);
			if(args.Length > 0)
			{
				if (args[0] == "-randomGame")
				{
					if (args.Length > 1)
					{
						switch(args[1])
						{
							case "register":
								if(args.Length > 2)
								{
									string steamId = args[2];

									SteamUser steamUser;
									ulong steamId64;
									if(ulong.TryParse(steamId, out steamId64))
									{
										steamUser = Steam.GetUser(steamId64);
									}
									else
									{
										steamUser = Steam.GetUser(steamId);
									}

									
									if(steamUser == null)
									{
										await message.Channel.SendMessageAsync("Invalid steamid or custom url.");
									}
									else
									{
										await message.Channel.SendMessageAsync($"Successfully set user.\n" +
											$"```SteamId64:{steamUser.SteamId64}\n" +
											$"Username :{steamUser.GetPlayerSummary().personaname}```");
										ulong DiscordId = message.Author.Id;
										ulong SteamId = steamUser.SteamId64;

										if (Links.ContainsKey(DiscordId))
										{
											Links[DiscordId] = SteamId;
										}
										else
										{
											Links.Add(DiscordId, SteamId);
										}
									}
								}
								else
								{
									await message.Channel.SendMessageAsync("Insufficent length.");
								}
								break;

							case "pick":
								await PickGame(message);
								break;

							case "list":
								await PickGame(message, false);
								break;

							case "help":
								string helpText = 
									"```\n" +
									"register: Links your steamid to your discord account.\n" +
									"pick    : Picks the game for you.\n" +
									"list    : Lists all common muliplayer games or all of the users games.\n" +
									"help    : Displays helptext.\n" +
									"```";

								await message.Channel.SendMessageAsync(helpText);
								break;
							default:
								await message.Channel.SendMessageAsync("Unknown argument.");
								break;
						}
					}
				}
			}
		}

		public Random_Steam_Game(PluginArgs pag) : base(pag)
		{
			Name = "Random Steam Game";

			if(PluginSection == null)
			{
				Console.WriteLine(
					$"{Name} won't function due to there being no section to add a steam key.\n" +
					$"If you want to get {Name} to function, add this to the discord.ini file\n" +
					$"[{Name}]\n" +
					$"steamKey=(Your steam key)");
			}
			else
			{
				if(PluginSection.HasKey("steamKey"))
				{
					Console.WriteLine("Found key.");
					Steam = new SteamClient(PluginSection.GetValue("steamKey"));
					UsedFunctions = Functions.MessageReceived;
				}
				else
				{
					Console.WriteLine(
						"Could not find steamKey in discord.ini file.\n" +
						$"In order for this to work, goto discord.ini, goto the [{Name}] section, and add steamKey=(Your steam key)"
						);
				}
			}
			Load();
			SaveL = Task.Run(SaveLoop);
		}
	}
}
