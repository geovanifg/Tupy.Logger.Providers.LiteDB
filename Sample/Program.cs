using System;
using Tupy;
using Tupy.Jobs;
using Tupy.Logger;
using Tupy.Logger.Providers.LiteDB;

namespace Sample
{
    class Program
    {
        private static bool stoprequested = false;
        private static JobManager jmgr = new JobManager();

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //you have 2 options here, leave e.Cancel set to false and just handle any
            //graceful shutdown that you can while in here, or set a flag to notify the other
            //thread at the next check that it's to shut down.  I'll do the 2nd option
            e.Cancel = true;
            stoprequested = true;
            Console.WriteLine("CancelKeyPress fired...");
        }

        static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            PrepareLog();

            LoggerOrchestrator.Start();

            ConfigureLogsToWrite();

            jmgr.Start();

            Console.WriteLine("Press Ctrl+C to exit!");

            while (!stoprequested)
            {

            }
            Console.WriteLine("Graceful shut down code here...");

            jmgr.Stop();

            LoggerOrchestrator.Stop();

            Console.ReadKey();
        }

        private static void PrepareLog()
        {
            var provtextf = new LiteDBProvider()
            {
                FolderPath = @"c:\temp",
                DbName= "log"
            };
            LoggerOrchestrator.ProviderManager.Add(provtextf);


            var srcinfinite = new EventSource()
            {
                Name = "RetentionPeriod: Infinite",
                MinimumRetention = 0
            };

            var src5minutes = new EventSource()
            {
                Name = "RetentionPeriod: FiveMinutes",
                MinimumRetention = 5,
                RetentionPeriodoType = FrequencyOptions.Minute
            };

            var src1hour = new EventSource()
            {
                Name = "RetentionPeriod: OneHour",
                MinimumRetention = 1,
                RetentionPeriodoType = FrequencyOptions.Hour
            };

            LoggerOrchestrator.AddEventSource(srcinfinite);
            LoggerOrchestrator.AddEventSource(src5minutes);
            LoggerOrchestrator.AddEventSource(src1hour);
        }

        private static void WriteErrorLog()
        {
            var src = "RetentionPeriod: Infinite";
            var type = EventEntryTypes.Error;
            var message = $"Error {DateTime.Now}";

            Logger.WriteEntry(src, message, type);
        }

        private static void WriteWarningLog()
        {
            var src = "RetentionPeriod: FiveMinutes";
            var type = EventEntryTypes.Warning;
            var message = $"Warning {DateTime.Now}";

            Logger.WriteEntry(src, message, type);
        }

        private static void WriteInfoLog()
        {
            var src = "RetentionPeriod: OneHour";
            var type = EventEntryTypes.Information;
            var message = $"Information {DateTime.Now}";

            Logger.WriteEntry(src, message, type);
        }

        private static void ConfigureLogsToWrite()
        {
            var joberror = new Job(FrequencyOptions.Minute, 1)
            {
                Name = "Job Erro",
                StepAction = delegate ()
                {
                    WriteErrorLog();
                },
                ReportStatus = Display
            };

            var jobwarning = new Job(FrequencyOptions.Minute, 1)
            {
                Name = "Job Warning",
                StepAction = delegate ()
                {
                    WriteWarningLog();
                },
                ReportStatus = Display
            };

            var jobinformation = new Job(FrequencyOptions.Minute, 1)
            {
                Name = "Job Information",
                StepAction = delegate ()
                {
                    WriteInfoLog();
                },
                ReportStatus = Display
            };


            jmgr.Jobs.Add(joberror);
            jmgr.Jobs.Add(jobwarning);
            jmgr.Jobs.Add(jobinformation);
        }

        static void Display(ExecutionResponse status)
        {
            Console.WriteLine($"{DateTime.Now} - {status.IsSuccess} - {status.Message} - {status.Content} - {status.Source}");
        }
    }
}
