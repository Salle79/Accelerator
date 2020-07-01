using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Litium.Events;
using Litium.Foundation;
using Litium.Foundation.Modules.ECommerce;
using Litium.Foundation.Modules.ExtensionMethods;
using Litium.Foundation.Security;
using Litium.Globalization;
using Litium.Owin.Lifecycle;
using Litium.Products;
using Litium.Products.Events;

namespace Litium.Accelerator.Search.Indexing.PopularProducts
{
    internal class MostSoldEventListener : IStartupTask
    {
        private readonly CategoryService _categoryService;
        private readonly EventBroker _eventBroker;
        private readonly MostSoldDataHolder _mostSoldDataHolder;
        private readonly SecurityToken _token;
        private readonly VariantService _variantService;
        private readonly ChannelService _channelService;
        private readonly MarketService _marketService;

        public MostSoldEventListener(MostSoldDataHolder mostSoldDataHolder, EventBroker eventBroker, CategoryService categoryService, VariantService variantService, ChannelService channelService, MarketService marketService)
        {
            _mostSoldDataHolder = mostSoldDataHolder;
            _eventBroker = eventBroker;
            _categoryService = categoryService;
            _variantService = variantService;
            _token = Solution.Instance.SystemToken;
            _channelService = channelService;
            _marketService = marketService;
        }

        public void Start()
        {
            ModuleECommerce.Instance.EventManager.StatisticUpdated += () => ThreadPool.QueueUserWorkItem(_ =>
            {
                this.Log().Trace("Running most sold update");
                try
                {
                    StatisticsUpdated();
                    this.Log().Trace("Ended most sold update");
                }
                catch (Exception e)
                {
                    var logger = this.Log();
                    logger.Error(e.Message, e);
                }
            });
        }

        private void StatisticsUpdated()
        {
            var articleStatistics = new List<Tuple<Guid, string, decimal>>();
            foreach (var channel in _channelService.GetAll())
            {
                if (channel.WebsiteSystemId !=null && channel.MarketSystemId != null)
                {
                    var market = _marketService.Get(channel.MarketSystemId.Value);
                    if (market?.AssortmentSystemId != null)
                    {
                       var categorySystemIds = _categoryService.GetChildCategories(Guid.Empty, market.AssortmentSystemId).SelectMany(x => x.GetChildren(true)).Select(x => x.SystemId).ToList();
                        articleStatistics.AddRange(ModuleECommerce.Instance.Statistics.GetMostSoldArticles(null, channel.WebsiteSystemId.Value, channel.SystemId, categorySystemIds, int.MaxValue, _token)
                            .GroupBy(x => x.VariantArticleNumber, StringComparer.OrdinalIgnoreCase)
                            .Select(x => new Tuple<Guid, string, decimal>(channel.WebsiteSystemId.Value, x.Key, x.Sum(z => z.Count))));
                    }
                }
            }

            var existingArticleNumbers = _mostSoldDataHolder.GetCurrentArticleNumbers();
            foreach (var item in articleStatistics.GroupBy(x => x.Item2))
            {
                if (!existingArticleNumbers.TryGetValue(item.Key, out HashSet<Guid> existingWebsite))
                {
                    existingWebsite = new HashSet<Guid>();
                }

                var isUpdated = item.Aggregate(false, (current, iitem) =>
                {
                    existingWebsite.Remove(iitem.Item1);
                    decimal old = -1;
                    if (_mostSoldDataHolder.TryGet(iitem.Item2, out IDictionary<Guid, decimal> c))
                    {
                        c.TryGetValue(iitem.Item1, out old);
                    }
                    var r = _mostSoldDataHolder.AddOrUpdate(iitem.Item2, iitem.Item1, iitem.Item3);
                    this.Log().Trace("WebSite: {3} Article: {0} IsUpdated: {2} IsCurrent: {5} OldCount: {4} Count: {1}", iitem.Item2, iitem.Item3, r, iitem.Item1, old, current);
                    return r | current;
                });

                this.Log().Trace("Article: {0} IsUpdated: {1}", item.Key, isUpdated);
                if (isUpdated)
                {
                    this.Log().Trace("Article: {0} try get", item.Key);
                    var variant = _variantService.Get(item.Key);
                    if (variant != null)
                    {
                        this.Log().Trace("Article: {0} fire event", item.Key);
                        _eventBroker.Publish(new VariantUpdated(variant.SystemId, variant.BaseProductSystemId, new Lazy<Variant>(() => variant)));
                    }
                    else
                    {
                        this.Log().Trace("Article: {0} did not exists", item.Key);
                    }
                }
            }

            foreach (var item in existingArticleNumbers.Where(x => x.Value.Count > 0))
            {
                foreach (var iitem in item.Value)
                {
                    _mostSoldDataHolder.Remove(item.Key, iitem);
                }

                var variant = _variantService.Get(item.Key);
                if (variant != null)
                {
                    this.Log().Trace("Article: {0} fire event", item.Key);
                    _eventBroker.Publish(new VariantUpdated(variant.SystemId, variant.BaseProductSystemId, new Lazy<Variant>(() => variant)));
                }
            }
        }
    }
}
