using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDbCloner
{
    public class CosmosDbHelper
    {
        private Container? _srcContainer;
        private Container? _targetContainer;
        private int throughput = 50000;

        public async Task<bool> SetUpDbsAndContainers(AppSettings appSettings)
        {
            try
            {
                Console.WriteLine("Fetching Src Container...");
                _srcContainer = await GetReadContainer(appSettings.SrcEndpointUri, appSettings.SrcPrimaryKey, appSettings.SrcDatabaseId, appSettings.SrcContainerId);
                Console.WriteLine("Fetching Target Container...");
                _targetContainer = await GetContainer(appSettings.TargetEndpointUri, appSettings.TargetPrimaryKey, appSettings.TargetDatabaseId, appSettings.TargetContainerId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception" + ex.Message);
                return false;
            }
        }

        public async Task<IReadOnlyCollection<CosmoDbEventDataModel>?> FetchEventsFromSrcAsync()
        {
            if (_srcContainer != null)
            {
                Console.WriteLine("Fetching Events...");
                var cancelToken = new CancellationTokenSource();
                var query = new QueryDefinition("SELECT * FROM c");
                return await ExecuteQuery<CosmoDbEventDataModel>(_srcContainer, query, null, cancelToken.Token).ConfigureAwait(false);
            }
            return null;
        }

        public async Task<Container?> GetReadContainer(string? EndpointUri, string? PrimaryKey, string? DatabaseId, string? ContainerId)
        {
            if (
                DatabaseId == null ||
                EndpointUri == null ||
                PrimaryKey == null ||
                ContainerId == null)
            {
                return null;
            }
            var cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            var db = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
            return await db.Database.CreateContainerIfNotExistsAsync(ContainerId, "/" + nameof(CosmoDbEventDataModel.AggregateId));
        }

        public async Task<Container?> GetContainer(string? EndpointUri, string? PrimaryKey, string? DatabaseId, string? ContainerId)
        {
            if (
                DatabaseId == null ||
                EndpointUri == null ||
                PrimaryKey == null ||
                ContainerId == null)
            {
                return null;
            }
            var cosmosClient = new CosmosClient(
            EndpointUri,
            PrimaryKey,
            new CosmosClientOptions()
            {
                ApplicationName = "Statsh",
                AllowBulkExecution = true
            });
            var db = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
            var container = await db.Database.DefineContainer(ContainerId, "/" + nameof(CosmoDbEventDataModel.AggregateId))
                    .WithIndexingPolicy()
                        .WithIndexingMode(IndexingMode.Consistent)
                        .WithIncludedPaths()
                            .Path("/oldts/*")
                            .Attach()
                        .WithExcludedPaths()
                            .Path("/*")
                            .Attach()
                    .Attach()
                .CreateAsync(throughput);
            return container.Container;
        }

        public async Task<IReadOnlyCollection<TOutput>> ExecuteQuery<TOutput>(
            Container container,
            QueryDefinition query,
            PartitionKey? partitionKey,
            CancellationToken cancellationToken)
        {
            var options = new QueryRequestOptions()
            {
                PartitionKey = partitionKey
            };
            var result = new List<TOutput>();
            using (var iterator = container.GetItemQueryIterator<TOutput>(query, null, options))
            {
                while (iterator.HasMoreResults)
                {
                    var items = await iterator.ReadNextAsync(cancellationToken);
                    result.AddRange(items);
                }
            }
            return result;
        }

        public async Task<bool> CommitEventsToContainer(IReadOnlyCollection<CosmoDbEventDataModel>? srcEvents)
        {
            if (_targetContainer != null)
            {
                int eventsCount = srcEvents?.Count ?? 0;
                var i = 0;
                Console.WriteLine("Started adding " + eventsCount + " events");
                List<Task> tasks = new List<Task>(eventsCount);
                ItemRequestOptions requestOptions = new ItemRequestOptions() { EnableContentResponseOnWrite = false };

                foreach (var dbEv in srcEvents ?? new List<CosmoDbEventDataModel>())
                {
                    var task = _targetContainer.CreateItemAsync(dbEv, new PartitionKey(dbEv.AggregateId), requestOptions);
                    tasks.Add(task);
                    i++;
                    Console.WriteLine(i + "/" + eventsCount);
                }
                // Wait until all are done
                await Task.WhenAll(tasks);
                return true;
            }
            return false;
        }

    }
}

//public async Task<bool> ReadEventsAndCommitToTarget()
//{
//    if (_srcContainer == null || _targetContainer == null)
//    {
//        return false;
//    }
//    Console.WriteLine("Started work...");
//    var cancelToken = new CancellationTokenSource();
//    var query = new QueryDefinition("SELECT * FROM c");
//    List<Task> tasks = new List<Task>();
//    var i = 0;
//    using (var iterator = _srcContainer.GetItemQueryIterator<CosmoDbEventDataModel>(query))
//    {
//        while (iterator.HasMoreResults)
//        {
//            var dbEvs = await iterator.ReadNextAsync(cancelToken.Token).ConfigureAwait(false);
//            foreach (var dbEv in dbEvs)
//            {
//                i++;
//                Console.WriteLine("Set Task For " + i);
//                tasks
//               .Add(
//                   _targetContainer.CreateItemAsync(dbEv, new PartitionKey(dbEv.AggregateId))
//                   .ContinueWith(itemResponse =>
//                   {
//                       if (!itemResponse.IsCompletedSuccessfully)
//                       {
//                           var innerExceptions = itemResponse.Exception?.Flatten();
//                           if (innerExceptions != null)
//                           {
//                               Console.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()?.Message}.");
//                           }
//                       }
//                   })
//               );
//            }

//        }
//    }
//    // Wait until all are done
//    await Task.WhenAll(tasks);
//    return true;
//}