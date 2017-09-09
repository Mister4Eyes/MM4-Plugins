using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using HtmlAgilityPack;

using Discord.WebSocket;

using Mister4Eyes.ArgParser;

using MemeMachine4.PluginBase;


namespace Random_Ow_Picker
{
	struct Hero
	{
		public Hero(string name, string portrateurl)
		{
			Name		= name;
			PortrateUrl	= portrateurl;
		}

		public string Name			{ get; set; }
		public string PortrateUrl	{ get; set; }
	}

	public class Random_Ow_Picker : Plugin
	{
		Hero[] Heros;
		HtmlWeb web = new HtmlWeb();
		Random r = new Random();

		public Random_Ow_Picker(PluginArgs pag) : base(pag)
		{
			Name = "Random hero picker.";
			UsedFunctions = Functions.MessageReceived;
			Heros = GetHeros();
		}

		private Hero[] GetHeros()
		{
			HtmlDocument document = web.Load("https://playoverwatch.com/en-us/heroes/");
			List<Hero> listedHeros = new List<Hero>();

			HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//div[@id='heroes-selector-container']/div[@data-groups]");
			
			foreach(HtmlNode node in nodes)
			{
				Hero hero = new Hero();
				HtmlNode portrate = node.SelectSingleNode(node.XPath + "/a/img");

				hero.Name = node.InnerText;
				hero.PortrateUrl = portrate.Attributes["src"].Value;

				listedHeros.Add(hero);
			}

			return listedHeros.ToArray();
		}

		public async override Task MessageReceived(SocketMessage message)
		{
			string[] args = ArgumentParser.ParseArgumentString(message.Content);

			if(args[0] == "-randomHero")
			{
				Hero hero = Heros[r.Next(Heros.Length)];
				string retMessage = $"{hero.Name}\n{hero.PortrateUrl}";

				await message.Channel.SendMessageAsync(retMessage);
			}
		}
	}
}
