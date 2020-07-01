using Litium.Blocks;
using Litium.Blocks.Events;
using Litium.Data;
using Litium.Events;
using Litium.Search.Indexing;
using Litium.Security.Events;
using Litium.Websites;
using Litium.Websites.Events;
using System;
using Litium.Websites.Queryable;

namespace Litium.Accelerator.Search.Indexing.Pages
{
    public class PageEventListener : IIndexQueueHandlerRegistration
    {
        private readonly string _pageTypeString = typeof(Page).FullName;
        private readonly IndexQueueService _indexQueueService;
        private readonly DataService _dataService;

        public PageEventListener(EventBroker eventBroker, IndexQueueService indexQueueService, DataService dataService)
        {
            _indexQueueService = indexQueueService;
            _dataService = dataService;

            eventBroker.Subscribe<BlockUpdated>(x => QueueBlock(x.Item));

            eventBroker.Subscribe<PageCreated>(x => Queue(x.Item));
            eventBroker.Subscribe<PageUpdated>(x => Queue(x.Item));
            eventBroker.Subscribe<PageDeleted>(x => _indexQueueService.Enqueue(new IndexQueueItem<PageDocument>(x.SystemId) { Action = IndexAction.Delete }));

            eventBroker.Subscribe<PageMovedToTrash>(x => _indexQueueService.Enqueue(new IndexQueueItem<PageDocument>(x.SystemId) { Action = IndexAction.Delete }));
            eventBroker.Subscribe<PageRestoredFromTrash>(x => Queue(x.Item));

            eventBroker.Subscribe<AccessControlEntryAdded>(x => Queue(x.EntrySystemId, x.EntityType));
            eventBroker.Subscribe<AccessControlEntryRemoved>(x => Queue(x.EntrySystemId, x.EntityType));
        }

        private void QueueBlock(Block block)
        {
            if (block.Global)
            {
                using (var query = _dataService.CreateQuery<Page>().Filter(descriptor =>
                {
                    descriptor.GlobalBlockSystemId(block.SystemId);
                }))
                {
                    var pageSystemIds = query.ToSystemIdList();
                    foreach (var pageSystemId in pageSystemIds)
                    {
                        _indexQueueService.Enqueue(new IndexQueueItem<PageDocument>(pageSystemId));
                    }
                }
            }
        }

        private void Queue(Page page)
        {
            if (page.Status == Common.ContentStatus.Published)
            {
                _indexQueueService.Enqueue(new IndexQueueItem<PageDocument>(page.SystemId));
            }
            else
            {
                _indexQueueService.Enqueue(new IndexQueueItem<PageDocument>(page.SystemId) { Action = IndexAction.Delete });
            }
        }

        private void Queue(Guid pageSystemId, string type)
        {
            if (type == _pageTypeString)
            {
                _indexQueueService.Enqueue(new IndexQueueItem<PageDocument>(pageSystemId));
            }
        }
    }
}
