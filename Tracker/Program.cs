﻿using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Tracker.Models;
using Tracker.Workers;

namespace Tracker
{
    class Program
    {
        private static TimeSpan ResultsTrackerTimeSpan = new TimeSpan(0, int.Parse(ConfigurationManager.AppSettings["ResultsSamplePeriodInMinutes"]), 0);
        private static TimeSpan ImagesTrackerTimeSpan = new TimeSpan(0, int.Parse(ConfigurationManager.AppSettings["ImagesSamplePeriodInMinutes"]), 0);
        private static TimeSpan PreliveTrackerTimeSpan = new TimeSpan(0, int.Parse(ConfigurationManager.AppSettings["PreliveSamplePeriodInMinutes"]), 0);
        private static TimeSpan TeamsTrackerTimeSpan = new TimeSpan(0, int.Parse(ConfigurationManager.AppSettings["TeamsSamplePeriodInMinutes"]), 0);
        private static Task ResultsTask = new Task(() => ResultsWorker.ResultsWorkerInit(ResultsTrackerTimeSpan));
        private static Task ImagesTask = new Task(() => ImagesWorker.ImagesWorkerInit(ImagesTrackerTimeSpan));
        private static Task PreliveTask = new Task(() => PreliveWorker.PreliveWorkerInit(PreliveTrackerTimeSpan));
        private static Task TeamsTask = new Task(() => TeamsWorker.TeamsWorkerInit(TeamsTrackerTimeSpan));
        private static Task LiveTask = new Task(() => LiveWorker.LiveWorkerInit(TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["LiveSamplePeriodInSeconds"]))));
        private static bool _createTablesOnStartup = ConfigurationManager.AppSettings["CreateTablesOnStartup"] == "true";
        static void Main(string[] args)
        {
            if (_createTablesOnStartup)
            {
                try
                {
                    TrackerDBContext db = new TrackerDBContext();
                    RelationalDatabaseCreator dbCreator = (RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>();
                    dbCreator.CreateTables();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Some of the tables already exist, consider changing CreateTablesOnStartup to false from App.config");
                }
            }            
            ResultsTask.Start();
            ImagesTask.Start();
            PreliveTask.Start();
            TeamsTask.Start();
            if (ConfigurationManager.AppSettings["TrackLive"] == "true")
            {
                LiveTask.Start();
            }
            while (true)
            {

            }
            
        }

    }
}
