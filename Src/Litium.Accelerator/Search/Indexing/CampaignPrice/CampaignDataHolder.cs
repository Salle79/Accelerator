using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Litium.Foundation.Configuration;
using Litium.Foundation.Extenssions;
using Litium.Owin.Lifecycle;
using Litium.Runtime.DependencyInjection;

namespace Litium.Accelerator.Search.Indexing.CampaignPrice
{
    [Service(ServiceType = typeof(CampaignDataHolder), Lifetime = DependencyLifetime.Singleton)]
    public class CampaignDataHolder : IReleaseTask, IStartupTaskAsync
    {
        private readonly Lazy<CampaignHolderData> _campaignData;
        private readonly FileInfo _filePath;

        public CampaignDataHolder()
        {
            _filePath = new FileInfo(Path.Combine(GeneralConfig.Instance.SearchDirectory, "CampaignPriceFiltering.bin"));
            _campaignData = new Lazy<CampaignHolderData>(() => _filePath.LoadData<CampaignHolderData>()
                                                               ?? new CampaignHolderData
                                                               {
                                                                   Articles = new ConcurrentDictionary<Guid, List<FilterCampaignData>>(),
                                                                   Campaigns = new ConcurrentDictionary<Guid, HashSet<Guid>>()
                                                               });
        }

        public bool AddOrUpdateArticlePrice(Guid articleId, Guid campaignId, decimal price)
        {
            var result = true;
            _campaignData.Value.Articles.AddOrUpdate(articleId, new List<FilterCampaignData> { new FilterCampaignData { CampaignId = campaignId, Price = price } }, (_, c) =>
            {
                var item = c.Find(x => x.CampaignId == campaignId);
                if (item == null)
                {
                    c.Add(new FilterCampaignData { CampaignId = campaignId, Price = price });
                }
                else
                {
                    result = item.Price != price;
                    item.Price = price;
                }
                return c;
            });

            _campaignData.Value.Campaigns.GetOrAdd(campaignId, new HashSet<Guid>()).Add(articleId);
            return result;
        }

        public List<FilterCampaignData> GetArticleCampaigns(Guid articleId)
        {
            return _campaignData.Value.Articles.GetOrAdd(articleId, _ => new List<FilterCampaignData>());
        }

        public HashSet<Guid> GetCampaignArticles(Guid campaignId)
        {
            return _campaignData.Value.Campaigns.GetOrAdd(campaignId, _ => new HashSet<Guid>());
        }

        public bool RemoveArticlePrice(Guid articleId, Guid campaignId)
        {
            _campaignData.Value.Articles.AddOrUpdate(articleId, new List<FilterCampaignData>(), (_, c) =>
            {
                c.RemoveAll(x => x.CampaignId == campaignId);
                return c;
            });

            return _campaignData.Value.Campaigns.GetOrAdd(campaignId, new HashSet<Guid>()).Remove(articleId);
        }

        public void RemoveCampaign(Guid campaignId)
        {
            _campaignData.Value.Campaigns.TryRemove(campaignId, out HashSet<Guid> item);
        }

        void IReleaseTask.Release()
        {
            if (_campaignData.IsValueCreated)
            {
                _filePath.Persist(_campaignData.Value);
            }
        }

        Task IStartupTaskAsync.Start()
        {
            return Task.Run(() =>
            {
                var x = _campaignData.Value;
            });
        }

        [Serializable]
        private class CampaignHolderData
        {
            public ConcurrentDictionary<Guid, List<FilterCampaignData>> Articles { get; set; }
            public ConcurrentDictionary<Guid, HashSet<Guid>> Campaigns { get; set; }
        }
    }
}
