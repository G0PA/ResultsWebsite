﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using RabbitMQ.TransportObject;
using RabbitMQ.TrackerEssentials;
using static RabbitMQ.TrackerEssentials.Communication.Sports;
using RabbitMQ.RabbitMQ;
using Tracker.Sites.Zplay;

namespace Tracker.Sites
{
    public static class ZPlay
    {
        private static HttpClient _client;
        private static HttpClientHandler _handler;
        private static List<Link> _links;
        private static string _specificLoLUrl = "http://1zplay.com/api/lol_match/{0}?_={1}";
        private static string _specificDota2Url = "http://1zplay.com/api/dota2_match/{0}?_=1558706649155";
        private static HashSet<string> _csIds = new HashSet<string>();

        static ZPlay()
        {
            _handler = new HttpClientHandler();
            //_handler.UseProxy = true;
            //_handler.Proxy = new System.Net.WebProxy("151.106.10.108", 8080);          
            _client = new HttpClient(_handler);
        }
        private static string getEpochSeconds()
        {
            long epoch = (long) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            return epoch.ToString();
        }
        public static void GetActiveGameIds()
        {
            _links = new List<Link>();
            var response = _client.GetStringAsync($"http://1zplay.com/api/live_schedules?_={getEpochSeconds()}&category=all").Result;
            if (string.IsNullOrEmpty(response))
            {
                return;
            }
            var json = JArray.Parse(response);
            foreach(var game in json)
            {               
                var category = game["category"]?.ToString();
                var gameState = game["state"]?.ToString();
                if (!string.IsNullOrEmpty(category) && gameState != null && gameState == "start")
                {
                    SportEnum sport = SportEnum.Undefined;
                    Uri uri = null;
                    string league = string.Empty;
                    string homeTeam = string.Empty;
                    int bestOf = 0;
                    int mapNumber = 0;
                    int scoreHome = 0;
                    int scoreAway = 0;
                    switch (category)
                    {
                        case "csgo":
                            var websocketHandshake = game["csgo_schedule"].ToString();
                            _csIds.Add(websocketHandshake);
                            continue;
                        case "lol":
                            var id = game["live_match"]?["id"]?.ToString() ?? game["_id"].ToString();
                            bestOf = int.Parse(game["round"].ToString());
                            sport = SportEnum.LeagueOfLegends;
                            league = game["league"]["name"].ToString();
                            scoreHome = int.Parse(game["left_score"].ToString());
                            scoreAway = int.Parse(game["right_score"].ToString());
                            homeTeam = game["left_team"]["name"].ToString();
                            mapNumber = bestOf == 1 ? 1 : int.Parse(game["live_match"]["index"].ToString());
                            uri = new Uri(string.Format(_specificLoLUrl, id.Trim(), getEpochSeconds()));
                            _links.Add(new Link(sport, uri)
                            {
                                BestOf = bestOf,
                                ScoreHome = scoreHome,
                                ScoreAway = scoreAway,
                                HomeTeamName = homeTeam,
                                MapNumber = mapNumber,
                                LeagueName = league,
                            });
                            continue;
                        case "dota2":
                            var dotaIds = game["dota2_matches"].ToString().Replace("[", "").Replace("]", "").Trim().Split(",");
                            mapNumber = dotaIds.Length;
                            bestOf = int.Parse(game["round"].ToString());
                            league = game["league"]["name"].ToString();
                            sport = SportEnum.Dota2;
                            scoreHome = int.Parse(game["left_score"].ToString());
                            scoreAway = int.Parse(game["right_score"].ToString());
                            homeTeam = game["left_team"]["name"].ToString();
                            uri = new Uri(string.Format(_specificDota2Url, dotaIds[mapNumber-1].Trim()));
                            _links.Add(new Link(sport, uri, league, mapNumber, bestOf)
                            {
                                ScoreHome = scoreHome,
                                ScoreAway = scoreAway,
                                HomeTeamName = homeTeam,
                            });
                            continue;
                    }
                }
            }
        }
        public static void SendActiveGames()
        {
            foreach(var link in _links)
            {
                try
                {
                    LiveEvent live;
                    switch (link.Sport)
                    {
                        case SportEnum.Dota2:
                            live = ParseDota2(link);
                            if(live != null) 
                            {
                                live.UpdateDate = DateTime.UtcNow;
                                RabbitMQMessageSender.Send(live);
                            }
                            break;
                        case SportEnum.LeagueOfLegends:
                            live = ParseLeagueOfLegends(link);
                            if (live != null)
                            {
                                live.UpdateDate = DateTime.UtcNow;
                                RabbitMQMessageSender.Send(live);
                            }
                            break;

                    }
                    
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
        }
        private static LiveEvent ParseDota2(Link link)
        {
            var json = JObject.Parse(_client.GetStringAsync(link.Uri).Result);
            LiveEvent ev = new LiveEvent();
            ev.Sport = SportEnum.Dota2;
            ev.GameTime = int.Parse(json["game_time"].ToString());
            ev.MapNumber = link.MapNumber;
            ev.LeagueName = link.LeagueName;
            ev.BestOf = link.BestOf;
            switch (json["first_tower"].ToString())
            {
                case "radiant":
                    ev.FirstTower = 2;
                    break;
                case "dire":
                    ev.FirstTower = 1;
                    break;
            }
            switch (json["first_blood"].ToString())
            {
                case "radiant":
                    ev.FirstBlood = 2;
                    break;
                case "dire":
                    ev.FirstBlood = 1;
                    break;
            }
            LiveTeam homeTeam = new LiveTeam();
            homeTeam.WinsInSeries = link.ScoreHome;
            homeTeam.Players = new List<LivePlayer>();
            homeTeam.TeamName = json["radiant_team"]["name"].ToString();
            homeTeam.Gold = int.Parse(json["radiant"]["gold"].ToString());
            homeTeam.Kills = int.Parse(json["radiant"]["score"].ToString());

            LiveTeam awayTeam = new LiveTeam();
            awayTeam.WinsInSeries = link.ScoreAway;
            awayTeam.Players = new List<LivePlayer>();
            awayTeam.TeamName = json["dire_team"]["name"].ToString();
            awayTeam.Gold = int.Parse(json["dire"]["gold"].ToString());
            awayTeam.Kills = int.Parse(json["dire"]["score"].ToString());

            foreach (var player in json["players"])
            {
                try
                {
                    LivePlayer pl = new LivePlayer();
                    pl.Nickname = player["account"]["name"].ToString();
                    pl.ChampionName = player["hero"]["name_en"].ToString();
                    pl.ChampionImageUrl = player["hero"]["image"].ToString();
                    if (player["team"].ToString() == "radiant")
                    {
                        homeTeam.Players.Add(pl);
                    }
                    else
                    {
                        awayTeam.Players.Add(pl);
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }          
            if (link.HomeTeamName.ToLowerInvariant() == awayTeam.TeamName.ToLowerInvariant()) //hack for reversed team names
            {
                ev.HomeTeam = awayTeam;
                ev.AwayTeam = homeTeam;

                ev.HomeTeam.WinsInSeries = link.ScoreHome;
                ev.AwayTeam.WinsInSeries = link.ScoreAway;

                return ev;
            }

            ev.HomeTeam = homeTeam;
            ev.AwayTeam = awayTeam;
            return ev;
        }
        private static LiveEvent ParseLeagueOfLegends(Link link)
        {
            var json = JObject.Parse(_client.GetStringAsync(link.Uri).Result);
            LiveEvent ev = new LiveEvent();
            ev.Sport = SportEnum.LeagueOfLegends;
            ev.GameTime = int.Parse(json["game_time"].ToString());
            ev.MapNumber = link.MapNumber;
            ev.LeagueName = link.LeagueName;
            ev.BestOf = link.BestOf;
            switch (json["first_tower"].ToString())
            {
                case "red":
                    ev.FirstTower = 2;
                    break;
                case "blue":
                    ev.FirstTower = 1;
                    break;
            }
            switch (json["first_blood"].ToString())
            {
                case "red":
                    ev.FirstBlood = 2;
                    break;
                case "blue":
                    ev.FirstBlood = 1;
                    break;
            }
            LiveTeam homeTeam = new LiveTeam();
            homeTeam.WinsInSeries = link.ScoreHome;
            homeTeam.Players = new List<LivePlayer>();
            homeTeam.TeamName = json["blue_team"]["name"].ToString();
            homeTeam.Gold = int.Parse(json["blue"]["gold"].ToString());
            homeTeam.Kills = int.Parse(json["blue"]["score"].ToString());

            LiveTeam awayTeam = new LiveTeam();
            awayTeam.WinsInSeries = link.ScoreAway;
            awayTeam.Players = new List<LivePlayer>();
            awayTeam.TeamName = json["red_team"]["name"].ToString();
            awayTeam.Gold = int.Parse(json["red"]["gold"].ToString());
            awayTeam.Kills = int.Parse(json["red"]["score"].ToString());

            foreach (var player in json["blue"]["players"])
            {
                LivePlayer pl = new LivePlayer();
                pl.Nickname = player["name"].ToString();
                pl.ChampionName = player["hero"]["name"].ToString();
                pl.ChampionImageUrl = player["hero"]["image_url"].ToString();
                if (player["color"].ToString() == "blue")
                {
                    homeTeam.Players.Add(pl);
                }
            }
            foreach (var player in json["red"]["players"])
            {
                LivePlayer pl = new LivePlayer();
                pl.Nickname = player["name"].ToString();
                pl.ChampionName = player["hero"]["name"].ToString();
                pl.ChampionImageUrl = player["hero"]["image_url"].ToString();
                if (player["color"].ToString() == "red")
                {
                    awayTeam.Players.Add(pl);
                }
            }

            if (link.HomeTeamName.ToLowerInvariant() == awayTeam.TeamName.ToLowerInvariant()) //hack for reversed team names
            {
                ev.HomeTeam = awayTeam;
                ev.AwayTeam = homeTeam;

                ev.HomeTeam.WinsInSeries = link.ScoreHome;
                ev.AwayTeam.WinsInSeries = link.ScoreAway;

                return ev;
            }

            ev.HomeTeam = homeTeam;
            ev.AwayTeam = awayTeam;

            return ev;
        }
    }
}
