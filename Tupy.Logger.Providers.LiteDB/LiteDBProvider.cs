using LiteDB;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Tupy.Logger.Providers.LiteDB
{
    public class LiteDBProvider : FileBasedProvider, IProvider
    {
        public ProviderTypes Type { get => ProviderTypes.LocalDatabase; }
        public string ID { get; set; }
        public string FolderPath { get; set; }
        public string DbName { get; set; }

        //public LiteDBProvider()
        //{
        //    MapEntities();
        //}

        //private void MapEntities()
        //{
        //    var mapper = BsonMapper.Global;

        //    mapper.Entity<EventEntry>()
        //        .Id(x => x.EventEntryId)

        //        //.Ignore(x => x.DoNotSerializeThis) // ignore this property (do not store)
        //        //.Field(x => x.CustomerName, "cust_name"); // rename document field
        //        ;
        //}

        private string GetCompleteFileName()
        {
            var completepath = Path.Combine(FolderPath, DbName + ".db");

            return completepath;
        }

        private ExecutionResponse Write(EventEntry entry)
        {
            var result = new ExecutionResponse(true);
            BsonValue id = null;
            var filename = GetCompleteFileName();
            using (var db = new LiteDatabase($"Filename={filename}"))
            {
                var col = db.GetCollection<EventEntry>();
                id = col.Insert(entry);

                if (id != null)
                {
                    col.EnsureIndex(r => r.TimeGenerated);
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "Error during insert";
                }
            }

            return result;
        }

        public async Task<ExecutionResponse> WriteEntryAsync(EventEntry entry)
        {
            ExecutionResponse fn()
            {
                return Write(entry);
            }

            var result = await WriteFileOperationAsync(fn);

            return result;
        }

        public async Task<ExecutionResponse> RemoveBeforeAsync(string sourceName, DateTime date)
        {
            var result = new ExecutionResponse(true);
            var filename = GetCompleteFileName();

            await Task.Run(() =>
             {
                 using (var db = new LiteDatabase($"Filename={filename}"))
                 {
                     var col = db.GetCollection<EventEntry>();

                     col.Delete(r => r.Source == sourceName && r.TimeGenerated <= date);
                     col.EnsureIndex(r => r.TimeGenerated);
                 }
             });

            return result;
        }
    }
}
