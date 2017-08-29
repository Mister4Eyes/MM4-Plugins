using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OverwatchStatistics.src
{
	public class Hero : IComparable
	{
		public string Name = string.Empty;
		public double Hours = double.NaN;

		public int CompareTo(object obj)
		{
			if (obj is Hero)
			{
				Hero h2 = obj as Hero;
				if (double.IsNaN(Hours) || double.IsNaN(h2.Hours))
				{
					return 0;
				}
				return (int)Math.Round(Hours - h2.Hours);
			}
			else
			{
				return 0;
			}
			throw new NotImplementedException();
		}
	}

	public class Player
	{
		HtmlWeb web = new HtmlWeb();
		string url = null;
		public bool Exists { get; private set; }

		public Console PlayerConsole { get; private set; }
		public Region PlayerRegion { get; private set; }

		public enum Console
		{
			Pc,
			Playstation,
			XBox
		};
		public enum Region
		{
			None,
			UnitedStates,
			Korea,
			EuropeanUnion,

		};

		public Player(string name)
		{
			name = name.Replace('#', '-');

			//Just in case there are invalid characters
			name = WebUtility.HtmlEncode(name);

			//Set up with a list and with pc seperated so we can add and remove stuff quickly without changing much.
			//On release this stuff should be optimized out so it's just for astetics and readability of the code.
			List<string> extensions = new List<string>();

			//Us is first because in the context I'm using it most people who would use it are american.
			string[] regions =
			{
			"us/",
			"kr/",
			"eu/"
		};

			//Adds in pc first because 9 times out of 10 in this context it will be pc.
			foreach (string region in regions)
			{
				extensions.Add($"pc/{region}");
			}
			extensions.Add("psn/");
			extensions.Add("xbl/");

			Exists = false;

			for (int i = 0; i < extensions.Count; ++i)
			{
				string iUrl = $"https://playoverwatch.com/en-us/career/{extensions[i]}{name}";
				HttpWebRequest request = WebRequest.Create(iUrl) as HttpWebRequest;
				request.Method = "HEAD";

				HttpWebResponse response = null;
				try
				{
					response = request.GetResponse() as HttpWebResponse;
					//We did it boys! We found the thing! 
					//Screw you blizard and your retarted search function!
					if (response.StatusCode != HttpStatusCode.NotFound)
					{
						Exists = true;

						//Sets some meta stats
						if (i / regions.Length == 0)
						{
							PlayerRegion = (Region)i + 1;
							PlayerConsole = Console.Pc;
						}
						else
						{
							//There will always be two left. Also Consoles don't have regions.
							PlayerRegion = Region.None;
							if (i - regions.Length == 0)
							{
								PlayerConsole = Console.Playstation;
							}
							else
							{
								PlayerConsole = Console.XBox;
							}
						}
						url = iUrl;
						break;
					}
					response.Close();
				}
				catch { }
			}
		}

		private List<Hero> GetMainFromNodes(HtmlNodeCollection nodes)
		{
			try
			{
				List<Hero> Heros = new List<Hero>();
				foreach (HtmlNode node in nodes)
				{
					if (node.Attributes.AttributesWithName("style").Count() != 0)
					{
						continue;
					}
					double time;
					string timeText = node.SelectSingleNode($"{node.XPath}/div[@class='description']").InnerText;
					if (timeText == "--")
					{
						time = 0;
					}
					else
					{
						Match m = Regex.Match(timeText, @"(\d+) (.+)");
						time = double.Parse(m.Groups[1].Value);

						//Time is in hours and dosen't need to be accurate.
						if (m.Groups[2].Value.StartsWith("minute"))
						{
							time /= 60;
						}
						else if (m.Groups[2].Value.StartsWith("second"))
						{
							time /= 3600;
						}
					}

					Heros.Add(new Hero()
					{
						Name = node.SelectSingleNode($"{node.XPath}/div[@class='title']").InnerText,
						Hours = time
					});
				}
				return Heros;
			}
			catch
			{
				return null;
			}

		}
		public Tuple<Hero[], Hero[]> GetMains()
		{
			if (Exists)
			{
				HtmlDocument document = web.Load(url);
				//'competitive'
				HtmlNodeCollection nodes = document.DocumentNode.SelectNodes(
					"//div[@id='quickplay']/section/div/div[@data-category-id='overwatch.guid.0x0860000000000021']/div/div/div");
				List<Hero> Heros = GetMainFromNodes(nodes);
				nodes = document.DocumentNode.SelectNodes(
					"//div[@id='competitive']/section/div/div[@data-category-id='overwatch.guid.0x0860000000000021']/div/div/div");
				//Here to add in competative hours
				List<Hero> compHeros = GetMainFromNodes(nodes);

				for (int i = 0; i < Heros.Count; ++i)
				{
					for (int j = 0; j < compHeros.Count; ++j)
					{
						if (Heros[i].Name == compHeros[j].Name)
						{
							Heros[i].Hours += compHeros[j].Hours;
							continue;
						}
					}
				}

				Heros.Sort();

				//And now, we determine outliers.
				double Q1 = Heros[Heros.Count / 4].Hours;
				double Q3 = Heros[(Heros.Count * 3) / 4].Hours;
				double IQR = (Q3 - Q1) * 1.5;
				double OutlierThreshold = Q3 + IQR;

				List<Hero> Mains = new List<Hero>();
				foreach (Hero hero in Heros)
				{
					if (hero.Hours > OutlierThreshold)
					{
						Mains.Add(hero);
					}
				}

				return SmallGroupSplit.SplitSmallGroup(Mains.ToArray());
			}
			else
			{
				return null;
			}
		}
		public string GetContent()
		{
			return (Exists) ? web.Load(url).DocumentNode.OuterHtml : null;
		}
	}
}
