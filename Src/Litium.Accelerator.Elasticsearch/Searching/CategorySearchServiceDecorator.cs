﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Litium.Accelerator.Routing;
using Litium.Accelerator.Search;
using Litium.Accelerator.Utilities;
using Litium.Accelerator.ViewModels.Search;
using Litium.Framework.Search;
using Litium.Products;
using Litium.Runtime.AutoMapper;
using Litium.Runtime.DependencyInjection;
using Litium.Search;
using Litium.Web;
using Litium.Web.Customers.TargetGroups;
using Litium.Web.Customers.TargetGroups.Events;
using Litium.Web.Models.Globalization;
using Nest;

namespace Litium.Accelerator.Searching
{
    [ServiceDecorator(typeof(CategorySearchService))]
    internal class CategorySearchServiceDecorator : CategorySearchService
    {
        private readonly CategorySearchService _parent;
        private readonly SearchClientService _searchClientService;
        private readonly CategoryService _categoryService;
        private readonly UrlService _urlService;
        private readonly RequestModelAccessor _requestModelAccessor;
        private readonly PersonStorage _personStorage;

        public CategorySearchServiceDecorator(
            CategorySearchService parent,
            SearchClientService searchClientService,
            CategoryService categoryService,
            UrlService urlService,
            RequestModelAccessor requestModelAccessor,
            PersonStorage personStorage)
        {
            _parent = parent;
            _searchClientService = searchClientService;
            _categoryService = categoryService;
            _urlService = urlService;
            _requestModelAccessor = requestModelAccessor;
            _personStorage = personStorage;
        }

        public override SearchResponse Search(SearchQuery searchQuery)
        {
            if (!_searchClientService.IsConfigured)
            {
                return _parent.Search(searchQuery);
            }
            var assortmentSystemId = _requestModelAccessor.RequestModel.ChannelModel?.Channel?.MarketSystemId?.MapTo<MarketModel>()?.Market?.AssortmentSystemId;
            var fuzziness = searchQuery.Text.Length > 2 ? Fuzziness.EditDistance(2) : Fuzziness.Auto;
            return new ElasticSearchResponse<CategoryDocument>(_searchClientService
                .Search<CategoryDocument>(CultureInfo.CurrentUICulture, selector => selector
                     .From((searchQuery.PageNumber - 1) * searchQuery.PageSize)
                     .Size(searchQuery.PageSize)
                     .QueryWithPermission(q =>
                        q.PublishedOnChannel()
                        && q.Bool(b => b.Must(bf => bf.Term(t => t.Field(x => x.Assortment).Value(assortmentSystemId))))
                        && OrganizationPointer(q)
                        && (
                            q.Match(x => x.Field(z => z.Name).Query(searchQuery.Text).Fuzziness(fuzziness).Boost(2).SynonymAnalyzer())
                            || q.Match(x => x.Field(z => z.Content).Query(searchQuery.Text).Fuzziness(fuzziness).SynonymAnalyzer())
                        ))
                     .Source(x => x.Includes(z => z.Fields(p => p.CategorySystemId)))
                     )
               );
        }

        private QueryContainer OrganizationPointer(QueryContainerDescriptor<CategoryDocument> selector)
        {
            if (_personStorage.CurrentSelectedOrganization != null)
            {
                return (selector.Bool(b => b.Filter(bf => bf.Term(t => t.Field(x => x.Organizations).Value(Guid.Empty))))
                        || selector.Bool(b => b.Filter(bf => bf.Term(t => t.Field(x => x.Organizations).Value(_personStorage.CurrentSelectedOrganization.SystemId)))));
            }

            return selector.Bool(b => b.Filter(bf => bf.Term(t => t.Field(x => x.Organizations).Value(Guid.Empty))));
        }

        public override SearchResult Transform(SearchQuery searchQuery, SearchResponse searchResponse, bool includeScore = false)
        {
            if (!(searchResponse is ElasticSearchResponse<CategoryDocument> elasticSearchResponse))
            {
                return _parent.Transform(searchQuery, searchResponse, includeScore);
            }

            return new SearchResult
            {
                Items = new Lazy<IEnumerable<SearchResultItem>>(() =>
                {
                    IoC.Resolve<TargetGroupEngine>().Process(new SearchEvent
                    {
                        SearchText = searchQuery.Text,
                        TotalHits = searchResponse.TotalHitCount
                    });

                    var category = _categoryService.Get(elasticSearchResponse.Response.Hits.Select(x => x.Source.CategorySystemId));
                    var categoryLookup = includeScore ? elasticSearchResponse.Response.Hits.ToDictionary(x => x.Source.CategorySystemId, x => x.Score.GetValueOrDefault()) : null;

                    return category.Select(x => new CategorySearchResult
                    {
                        Item = x,
                        Id = x.SystemId,
                        Name = x.Localizations.CurrentUICulture.Name,
                        Url = _urlService.GetUrl(x),
                        Score = includeScore ? categoryLookup.TryGetValue(x.SystemId, out var dbl) ? (float)dbl : default : default
                    });
                }),
                PageSize = searchQuery.PageSize.Value,
                Total = searchResponse.TotalHitCount
            };
        }
    }
}
