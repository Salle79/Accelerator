using System;
using System.Collections.Concurrent;
using System.Linq;
using Litium.Events;
using Litium.Foundation;
using Litium.Foundation.Modules.ECommerce;
using Litium.Foundation.Modules.ECommerce.Plugins.Campaigns.Actions;
using Litium.Foundation.Modules.ECommerce.Plugins.Campaigns.Conditions;
using Litium.Foundation.Security;
using Litium.Owin.Lifecycle;
using Litium.Products;
using Litium.Products.Events;

namespace Litium.Accelerator.Search.Indexing.CampaignPrice
{
    internal class CampaignEventListener : IStartupTask
    {
        private readonly Action<Guid> _articleUpdated;
        private readonly CampaignDataHolder _campaignDataHolder;
        private readonly ConcurrentDictionary<Guid, object> _syncLocks = new ConcurrentDictionary<Guid, object>();
        private readonly SecurityToken _token;

        public CampaignEventListener(CampaignDataHolder campaignDataHolder, EventBroker eventBroker, VariantService variantService)
        {
            _campaignDataHolder = campaignDataHolder;
            _token = Solution.Instance.SystemToken;

            _articleUpdated = x =>
            {
                var variant = variantService.Get(x);
                if (variant != null)
                {
                    eventBroker.Publish(new VariantUpdated(variant.SystemId, variant.BaseProductSystemId, new Lazy<Variant>(() => variant)));
                }
            };
        }

        void IStartupTask.Start()
        {
            ModuleECommerce.Instance.EventManager.CampaignCreated += Updated;
            ModuleECommerce.Instance.EventManager.CampaignDeleted += Deleted;
            ModuleECommerce.Instance.EventManager.CampaignActionInfoUpdated += (campaignId, _) => Updated(campaignId);
        }

        private void Deleted(Guid campaignId)
        {
            foreach (var key in _campaignDataHolder.GetCampaignArticles(campaignId).ToList())
            {
                _campaignDataHolder.RemoveArticlePrice(key, campaignId);
            }
        }

        private void RemoveCampaign(Guid campaignId)
        {
            foreach (var key in _campaignDataHolder.GetCampaignArticles(campaignId).ToList())
            {
                if (_campaignDataHolder.RemoveArticlePrice(key, campaignId))
                {
                    _articleUpdated(key);
                }
            }

            _campaignDataHolder.RemoveCampaign(campaignId);
        }

        private void Updated(Guid campaignId)
        {
            lock (_syncLocks.GetOrAdd(campaignId, _ => new object()))
            {
                var campaign = ModuleECommerce.Instance.Campaigns.GetCampaign(campaignId, _token);
                if (campaign.ConditionInfos.All(x => x.TypeName == typeof(UserBelongsToGroupCondition).FullName))
                {
                    var campaignActionInfo = campaign.ActionInfo;
                    if (campaignActionInfo != null)
                    {
                        if (campaignActionInfo.TypeName == typeof(ArticleCampaignPriceAction).FullName)
                        {
                            var data = campaignActionInfo.GetData<ArticleCampaignPriceAction.Data>();
                            foreach (var key in _campaignDataHolder.GetCampaignArticles(campaignId).Except(data.CampaignPriceList.Keys).ToList())
                            {
                                if (_campaignDataHolder.RemoveArticlePrice(key, campaignId))
                                {
                                    _articleUpdated(key);
                                }
                            }

                            foreach (var item in data.CampaignPriceList)
                            {
                                if (_campaignDataHolder.AddOrUpdateArticlePrice(item.Key, campaignId, item.Value))
                                {
                                    _articleUpdated(item.Key);
                                }
                            }
                        }
                        else
                        {
                            RemoveCampaign(campaignId);
                        }
                    }
                }
                else
                {
                    RemoveCampaign(campaignId);
                }
            }
        }
    }
}
