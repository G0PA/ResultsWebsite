using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Models;
using Microsoft.Extensions.Configuration;
using WebApi.Communication;
using Tracker.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    public class LeagueOfLegendsController : Controller
    {
        private TrackerDBContext db;       
        private Object _lock = new Object();
        private IConfiguration Configuration;
        public LeagueOfLegendsController(TrackerDBContext db, IConfiguration Configuration)
        {  
            if (this.db == null)
            {
                this.db = db;
            }
            if(this.Configuration == null)
            {
                this.Configuration = Configuration;
            }
        }
        [HttpGet("[action]")]
        public Sport GetSport()
        {
            Sport sport = new Sport();
            ConcurrentDictionary<string, HashSet<Results>> ResultEvents = new ConcurrentDictionary<string, HashSet<Results>>();
            ConcurrentDictionary<string, HashSet<Prelive>> PreliveEvents = new ConcurrentDictionary<string, HashSet<Prelive>>();
            var results = db.Results.ToList();
            Parallel.ForEach(results, (result) =>
            {
                if (result.SportId == 1)
                {
                    if (ResultEvents.ContainsKey(result.LeagueName))
                    {
                        lock (_lock)
                        {
                            ResultEvents[result.LeagueName].Add(result);
                        }
                    }
                    else
                    {
                        lock (_lock)
                        {
                            ResultEvents.TryAdd(result.LeagueName, new HashSet<Results>() { result });
                        }
                    }

                }
            });
            sport.ResultsEvents = ResultEvents;
            ConcurrentDictionary<string, string> images = new ConcurrentDictionary<string, string>();
            var defaultImageByteArray = System.IO.File.ReadAllBytes
                              (Configuration.GetSection("ImagePathReader").Value + "defaultLoLLogo" + ".png");
            var defaultImageString = Convert.ToBase64String(defaultImageByteArray);
            images.TryAdd("default", defaultImageString);
            Parallel.ForEach(ResultEvents, (league) =>
            {
                var results2 = league.Value;
                foreach (var res in results2)
                {
                    try
                    {
                        if (!images.ContainsKey(res.HomeTeam))
                        {
                            var imageByteArray = System.IO.File.ReadAllBytes
                                (Configuration.GetSection("ImagePathReader").Value + res.HomeTeam + ".png");
                            var imageString = Convert.ToBase64String(imageByteArray);
                            images.TryAdd(res.HomeTeam.Trim(), imageString);
                        }
                        else if (!images.ContainsKey(res.AwayTeam))
                        {
                            var imageByteArray = System.IO.File.ReadAllBytes
                                                            (Configuration.GetSection("ImagePathReader").Value + res.AwayTeam + ".png");
                            var imageString = Convert.ToBase64String(imageByteArray);
                            images.TryAdd(res.AwayTeam.Trim(), imageString);
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            });
            sport.TeamLogos = images;

            var preliveEventsFromDb = db.Prelive.ToList();

            Parallel.ForEach(preliveEventsFromDb, (preliveEvent) =>
            {
                if (preliveEvent.SportId == 1)
                {
                    if (PreliveEvents.ContainsKey(preliveEvent.LeagueName))
                    {
                        lock (_lock)
                        {
                            PreliveEvents[preliveEvent.LeagueName].Add(preliveEvent);
                        }
                    }
                    else
                    {
                        lock (_lock)
                        {
                            PreliveEvents.TryAdd(preliveEvent.LeagueName, new HashSet<Prelive>() { preliveEvent });
                        }
                    }

                }
            });
            sport.PreliveEvents = PreliveEvents;
            return sport;

        }
        

    }
}
