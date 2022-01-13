using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CosmosDbCloner
{
    class Program
    {
        private static AppSettings? appSettings = null;
        static async Task Main(string[] args)
        {
            DateTime dt = DateTime.Now;
            var cosmosHelper = new CosmosDbHelper();
            using (StreamReader r = new StreamReader("AppSettings.json"))
            {
                string json = r.ReadToEnd();
                appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
                var isSetupOk = await cosmosHelper.SetUpDbsAndContainers(appSettings);
                if (!isSetupOk) return;
                var srcEvents = await cosmosHelper.FetchEventsFromSrcAsync();
                var res = await cosmosHelper.CommitEventsToContainer(srcEvents);
                //var res = await cosmosHelper.ReadEventsAndCommitToTarget();
                if (res)
                {
                    TimeSpan ts = DateTime.Now - dt;
                    Console.WriteLine("All is fine " + ts.TotalMilliseconds.ToString() + "elapsed");
                }
            }
        }

    }
}
