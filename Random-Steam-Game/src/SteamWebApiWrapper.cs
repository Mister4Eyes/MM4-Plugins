using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using RestSharp;
using HtmlAgilityPack;

namespace Random_Steam_Game
{
	//A root object will always contain a Response
	public class RootObject<T>
	{
		public T response { get; set; }
	}

	#region Responses
	public class ResolveVanityUrl_Response
	{
		public string steamid { get; set; }
		public int success { get; set; }
	}

	public class GetPlayerLevel_Response
	{
		public int player_level { get; set; }
	}

	public class GetPlayerSummaries_Response
	{
		public List<Player> players { get; set; }
	}

	public class GetOwnedGame_Response
	{
		public int game_count { get; set; }
		public List<Game> games { get; set; }
	}
	#endregion

	#region GameData
	public class Appid
	{
		public bool success { get; set; }
		public Data data { get; set; }
	}

	public class Data
	{
		public string type { get; set; }
		public string name { get; set; }
		public int steam_appid { get; set; }
		public int required_age { get; set; }
		public int[] dlc { get; set; }
		public string detailed_description { get; set; }
		public string about_the_game { get; set; }
		public Fullgame fullgame { get; set; }
		public string supported_languages { get; set; }
		public string header_image { get; set; }
		public string website { get; set; }
		public Pc_Requirements pc_requirements { get; set; }
		public Mac_Requirements mac_requirements { get; set; }
		public Linux_Requirements linux_requirements { get; set; }
		public string[] developers { get; set; }
		public Demos demos { get; set; }
		public Price_Overview price_overview { get; set; }
		public int[] packages { get; set; }
		public Package_Groups[] package_groups { get; set; }
		public Platforms platforms { get; set; }
		public Metacritic metacritic { get; set; }
		public Category[] categories { get; set; }
		public Screenshot[] screenshots { get; set; }
		public Movie[] movies { get; set; }
		public Recommendations recommendations { get; set; }
		public Achievements achievements { get; set; }
		public Release_Date release_date { get; set; }
	}

	public class Fullgame
	{
		public int appid { get; set; }
		public string name { get; set; }
	}

	public class Pc_Requirements
	{
		public string minimum { get; set; }
		public string recommended { get; set; }
	}

	public class Mac_Requirements
	{
		public string minimum { get; set; }
		public string recommended { get; set; }
	}

	public class Linux_Requirements
	{
		public string minimum { get; set; }
		public string recommended { get; set; }
	}

	public class Demos
	{
		public int appid { get; set; }
		public string description { get; set; }
	}

	public class Price_Overview
	{
		public string currency { get; set; }
		public int initial { get; set; }
		public int final { get; set; }
		public int discount_precent { get; set; }
	}

	public class Platforms
	{
		public bool windows { get; set; }
		public bool mac { get; set; }
		public bool linux { get; set; }
	}

	public class Metacritic
	{
		public int score { get; set; }
		public string url { get; set; }
	}

	public class Recommendations
	{
		public int total { get; set; }
	}

	public class Achievements
	{
		public int total { get; set; }
		public Highlighted[] highlighted { get; set; }
	}

	public class Highlighted
	{
		public string name { get; set; }
		public string path { get; set; }
	}

	public class Release_Date
	{
		public bool coming_soon { get; set; }
		public string date { get; set; }
	}

	public class Package_Groups
	{
		public string name { get; set; }
		public string title { get; set; }
		public string description { get; set; }
		public string selection_text { get; set; }
		public string save_text { get; set; }
		public int display_type { get; set; }
		public string is_recurring_subscription { get; set; }
		public Sub[] subs { get; set; }
	}

	public class Sub
	{
		public int packageid { get; set; }
		public string percent_savings_text { get; set; }
		public int percent_savings { get; set; }
		public string option_text { get; set; }
		public string option_description { get; set; }
	}

	public class Category
	{
		public int id { get; set; }
		public string description { get; set; }
	}

	public class Screenshot
	{
		public int id { get; set; }
		public string path_thumbnail { get; set; }
		public string path_full { get; set; }
	}

	public class Movie
	{
		public int id { get; set; }
		public string name { get; set; }
		public string thumbnail { get; set; }
		public Webm webm { get; set; }
	}

	public class Webm
	{
		public string _480 { get; set; }
		public string max { get; set; }
	}

	public class PcRequirements
	{
		public string minimum { get; set; }
	}
	#endregion

	public class Player
	{
		public string steamid { get; set; }
		public int communityvisibilitystate { get; set; }
		public int profilestate { get; set; }
		public string personaname { get; set; }
		public int lastlogoff { get; set; }
		public int commentpermission { get; set; }
		public string profileurl { get; set; }
		public string avatar { get; set; }
		public string avatarmedium { get; set; }
		public string avatarfull { get; set; }
		public int personastate { get; set; }
		public string realname { get; set; }
		public string primaryclanid { get; set; }
		public int timecreated { get; set; }
		public int personastateflags { get; set; }
		public string loccountrycode { get; set; }
	}

	public class Game
	{
		public int appid { get; set; }
		public string name { get; set; }
		public int playtime_forever { get; set; }
		public string img_icon_url { get; set; }
		public string img_logo_url { get; set; }
		public bool has_community_visible_stats { get; set; }
		public int? playtime_2weeks { get; set; }
	}


	public class SteamClient
	{
		string Key;
		RestClient client = new RestClient("http://api.steampowered.com/");
		RestClient storeClient = new RestClient("http://store.steampowered.com/api/");
		HtmlWeb web = new HtmlWeb();
		Dictionary<int, bool> multiplayerStats = new Dictionary<int, bool>();

		public SteamClient(string steamKey)
		{
			Key = steamKey;
		}

		public SteamUser GetUser(ulong SteamId64)
		{
			SteamUser SU = new SteamUser(SteamId64, this);
			if (SU.userExists)
			{
				return SU;
			}
			else
			{
				return null;
			}
		}

		//Kept seperate just in case it's needed elsewhere in the future.
		public static int SteamBool(bool b)
		{
			return (b) ? 1 : 0;
		}

		//This function's whole purpose is to reduce redundency in code.
		public object GeneralGetRequestHandler<T>(
			string url,
			Tuple<string, object>[] ValueParameters,
			Func<T, object> finalReturn)
		{
			RestRequest request = new RestRequest(url, Method.GET);

			//Key is used in everything. So it's added by default
			request.AddParameter("key", Key);
			foreach (Tuple<string, object> pair in ValueParameters)
			{
				request.AddParameter(pair.Item1, pair.Item2);
			}

			IRestResponse<RootObject<T>> root = client.Execute<RootObject<T>>(request);

			return finalReturn(root.Data.response);
		}

		//Returns a game with it's appid and name.
		public SteamGame GetSteamGame(int appid)
		{
			Game g = new Game();
			g.appid = appid;

			//More webscraping! Glad that the data function is broken!
			string url = $"http://store.steampowered.com/app/{appid}";
			HtmlDocument doc = web.Load(url);
			HtmlNode name = doc.DocumentNode.SelectSingleNode("//div[@class='apphub_AppName']");
			g.name = name.InnerText;

			return new SteamGame(g, this);
		}

		public Data GetGameData(SteamGame game)
		{
			return GetGameData(game.appid);
		}

		public Data GetGameData(Game game)
		{
			return GetGameData(game.appid);
		}

		public Data GetGameData(int appid)
		{
			RestRequest restReq = new RestRequest("appdetails/", Method.GET);
			restReq.AddParameter("appids", appid);

			IRestResponse<RootObject<Appid>> response = storeClient.Execute<RootObject<Appid>>(restReq);

			Appid trueRes = response.Data.response;
			return (trueRes == null) ? null : trueRes.data;
		}

		public bool HasMultiplayer(int appid)
		{
			//Used to keep things fast.
			if (multiplayerStats.ContainsKey(appid))
			{
				return multiplayerStats[appid];
			}
			//Good ol fasioned web scraping.
			string url = $"http://store.steampowered.com/app/{appid}";
			HtmlDocument doc = web.Load(url);

			HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@id='category_block']/div/a");

			if(nodes != null)
			{
				foreach (HtmlNode node in nodes)
				{
					if (node.InnerText == "Multi-player")
					{
						multiplayerStats.Add(appid, true);
						return true;
					}
				}
			}

			multiplayerStats.Add(appid, false);
			return false;
		}

		public SteamUser GetUser(string vanityUrl)
		{
			return (SteamUser)GeneralGetRequestHandler<ResolveVanityUrl_Response>(
				"ISteamUser/ResolveVanityURL/v0001/",
				new[]{
					new Tuple<string, object>("vanityurl", vanityUrl)
				},
				(response) =>
				{
					if (response == null || response.success != 1)
					{
						return null;
					}
					SteamUser su = new SteamUser(ulong.Parse(response.steamid), this);
					return (su.userExists) ? su : null;
				});
		}

		private SteamGame[] GameToSteamGame(Game[] game)
		{
			SteamGame[] sGame = new SteamGame[game.Length];

			for (int i = game.Length - 1; i >= 0; --i)
			{
				sGame[i] = new SteamGame(game[i], this);
			}

			return sGame;
		}

		public SteamGame[] GetGames(ulong SteamId64, bool includeAppinfo = false, bool includePayedFreeGames = false, int[] appidFilter = null)
		{
			List<Tuple<string, object>> Keys = new List<Tuple<string, object>>();
			Keys.AddRange(new[]
				{
					new Tuple<string, object>("steamid", SteamId64),
					new Tuple<string, object>("include_appinfo", SteamBool(includeAppinfo)),
					new Tuple<string, object>("include_played_free_games", SteamBool(includePayedFreeGames))
				});

			//Yes, this is how valve designed this. See https://wiki.teamfortress.com/wiki/WebAPI/GetOwnedGames
			if (appidFilter != null)
			{
				for (int i = 0; i < appidFilter.Length; ++i)
				{
					Keys.Add(new Tuple<string, object>($"appids_filter[{i}]", appidFilter[i]));
				}
			}

			return (SteamGame[])GeneralGetRequestHandler<GetOwnedGame_Response>(
				"IPlayerService/GetOwnedGames/v1/",
				Keys.ToArray(),
				(response) =>
				{
					return (response == null) ? null : GameToSteamGame(response.games.ToArray());
				});
		}

		public Player GetPlayerSummery(ulong SteamId64)
		{
			return (Player)GeneralGetRequestHandler<GetPlayerSummaries_Response>(
				"ISteamUser/GetPlayerSummaries/v0002/",
				new[]
				{
					new Tuple<string, object>("steamids", SteamId64)
				},
				(response) =>
				{
					return (response == null) ? null : response.players[0];
				});
		}

		public int GetSteamLevel(ulong SteamId64)
		{
			return (int)GeneralGetRequestHandler<GetPlayerLevel_Response>(
				"IPlayerService/GetSteamLevel/v1",
				new[]
				{
					new Tuple<string, object>("steamid", SteamId64)
				},
				(response) =>
				{
					if (response == null)
					{
						return -1;
					}
					else
					{
						return response.player_level;
					}
				});
		}

		public int GetAmmountGamesOwned(ulong SteamId64, bool includePlayedFreeGames)
		{
			return (int)GeneralGetRequestHandler<GetOwnedGame_Response>(
				"IPlayerService/GetOwnedGames/v1/",
				new[]
				{
					new Tuple<string, object>("steamid", SteamId64),
					new Tuple<string, object>("include_played_free_games", SteamBool(includePlayedFreeGames))
				},
				(response) =>
				{

					return (response == null) ? -1 : response.game_count;
				});
		}
	}

	public class SteamGame
	{
		Game game;
		SteamClient steamClient;
		private bool? hasMultiplayerContainer = null;

		public bool HasMultiplayer
		{
			get
			{
				if (hasMultiplayerContainer == null)
				{
					hasMultiplayerContainer = steamClient.HasMultiplayer(appid);
				}

				//Due to previous statement, it will never run into any issues.
				return (bool)hasMultiplayerContainer;
			}
		}
		public SteamGame(Game g, SteamClient sc)
		{
			game = g;
			steamClient = sc;
		}

		public Data GetGameData()
		{
			return steamClient.GetGameData(this);
		}

		//Game parametres
		public int appid
		{
			get
			{
				return game.appid;

			}
		}

		public string name
		{
			get
			{
				return game.name;
			}
		}

		public int playtime_forever
		{
			get
			{
				return game.playtime_forever;
			}
		}

		public string img_icon_url
		{
			get
			{
				return game.img_icon_url;
			}
		}

		public string img_logo_url
		{
			get
			{
				return game.img_logo_url;
			}
		}

		public bool has_community_visible_stats
		{
			get
			{
				return game.has_community_visible_stats;
			}
		}

		public int? playtime_2weeks
		{
			get
			{
				return game.playtime_2weeks;
			}
		}
	}

	public class SteamUser
	{
		public ulong SteamId64;
		public bool userExists;
		SteamClient Sc;
		public SteamUser(ulong steamId64, SteamClient sc)
		{
			Sc = sc;
			SteamId64 = steamId64;
			userExists = -1 != GetSteanLevel();
		}

		public int GetSteanLevel()
		{
			return Sc.GetSteamLevel(SteamId64);
		}

		public SteamGame[] GetGames(bool includeAppInfo = true, bool includePayedFreeGames = true, int[] appidFilter = null)
		{
			return Sc.GetGames(SteamId64, includeAppInfo, includePayedFreeGames, appidFilter);
		}

		public int GetAmmountGamesOwned(bool includePlayedFreeGames = true)
		{
			return Sc.GetAmmountGamesOwned(SteamId64, includePlayedFreeGames);
		}

		public Player GetPlayerSummary()
		{
			return Sc.GetPlayerSummery(SteamId64);
		}
	}
}
