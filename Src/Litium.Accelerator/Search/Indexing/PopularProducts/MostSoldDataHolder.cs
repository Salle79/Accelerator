using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Litium.Foundation.Configuration;
using Litium.Owin.Lifecycle;
using Litium.Runtime.DependencyInjection;
using Litium.Studio.Extenssions;

namespace Litium.Accelerator.Search.Indexing.PopularProducts
{
    [Service(ServiceType = typeof(MostSoldDataHolder), Lifetime = DependencyLifetime.Singleton)]
    public class MostSoldDataHolder : IReleaseTask, IStartupTaskAsync
    {
        private readonly FileInfo _filePath;
        private readonly Lazy<MostSoldDataHolderData> _mostSoldData;

        public MostSoldDataHolder()
        {
            _filePath = new FileInfo(Path.Combine(GeneralConfig.Instance.SearchDirectory, "MostSoldFiltering.bin"));
            _mostSoldData = new Lazy<MostSoldDataHolderData>(() => _filePath.LoadData<MostSoldDataHolderData>()
                                                                   ?? new MostSoldDataHolderData
                                                                   {
                                                                       Data = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, decimal>>(StringComparer.OrdinalIgnoreCase)
                                                                   });
        }

        public bool AddOrUpdate(string articleNumber, Guid webSiteId, decimal count)
        {
            var isChanged = false;

            _mostSoldData.Value.Data
                         .GetOrAdd(articleNumber, _ => new ConcurrentDictionary<Guid, decimal>())
                         .AddOrUpdate(webSiteId, _ =>
                         {
                             isChanged = true;
                             return count;
                         }, (_, o) =>
                         {
                             if (o == count)
                             {
                                 return o;
                             }

                             isChanged = true;
                             return count;
                         });

            return isChanged;
        }

        public Dictionary<string, HashSet<Guid>> GetCurrentArticleNumbers()
        {
            return _mostSoldData.Value.Data.ToDictionary(x => x.Key, x => new HashSet<Guid>(x.Value.Keys));
        }

        public void Remove(string articleNumber, Guid iitem)
        {
            if (_mostSoldData.Value.Data.TryRemove(articleNumber, out ConcurrentDictionary<Guid, decimal> item))
            {
                item.TryRemove(iitem, out decimal value);
            }
        }

        public bool TryGet(string articleNumber, out IDictionary<Guid, decimal> items)
        {
            if (_mostSoldData.Value.Data.TryGetValue(articleNumber, out ConcurrentDictionary<Guid, decimal> item))
            {
                items = item;
                return true;
            }

            items = null;
            return false;
        }

        void IReleaseTask.Release()
        {
            this.Log().Trace("");
            if (_mostSoldData.IsValueCreated)
            {
                _filePath.Persist(_mostSoldData.Value);
            }
        }

        Task IStartupTaskAsync.Start()
        {
            return Task.Run(() =>
            {
                var x = _mostSoldData.Value;
            });
        }

        [Serializable]
        private class MostSoldDataHolderData
        {
            public ConcurrentDictionary<string, ConcurrentDictionary<Guid, decimal>> Data { get; set; }
        }
    }
}
