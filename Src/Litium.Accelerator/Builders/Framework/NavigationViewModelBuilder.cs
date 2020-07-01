﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Litium.Accelerator.Caching;
using Litium.Accelerator.Constants;
using Litium.Accelerator.Extensions;
using Litium.Accelerator.Routing;
using Litium.Accelerator.Search;
using Litium.Accelerator.Search.Filtering;
using Litium.Accelerator.ViewModels.Framework;
using Litium.FieldFramework;
using Litium.FieldFramework.FieldTypes;
using Litium.Foundation.Modules.ExtensionMethods;
using Litium.Products;
using Litium.Runtime.AutoMapper;
using Litium.Security;
using Litium.Web;
using Litium.Web.Models;
using Litium.Web.Models.Products;
using Litium.Web.Models.Websites;
using Litium.Web.Rendering;
using Litium.Web.Routing;
using Litium.Websites;

namespace Litium.Accelerator.Builders.Framework
{
    /// <summary>
    /// Control builds mega menu.
    /// Mega menu could work in three ways.
    /// 1. Shows simple links.
    /// 2. Shows mega menu content.
    /// 3. Shows mega menu subpages.
    /// </summary>
    public class NavigationViewModelBuilder : IViewModelBuilder<NavigationViewModel>
    {
        private readonly RequestModelAccessor _requestModelAccessor;
        private readonly CategoryService _categoryService;
        private readonly PageService _pageService;
        private readonly UrlService _urlService;
        private readonly PageByFieldTemplateCache<MegaMenuPageFieldTemplateCache> _pageByFieldType;
        private readonly AuthorizationService _authorizationService;
        private readonly List<Guid> _selectedStructureId = new List<Guid>();
        private readonly RouteRequestInfoAccessor _routeRequestInfoAccessor;
        private readonly FilterService _filterService;
        private readonly ContentProcessorService _contentProcessorService;
        private readonly FilterAggregator _filterAggregator;
        private readonly Guid _channelSystemId;
        private readonly Guid _websiteSystemId;

        public NavigationViewModelBuilder(RequestModelAccessor requestModelAccessor,
            CategoryService categoryService,
            PageService pageService,
            UrlService urlService,
            PageByFieldTemplateCache<MegaMenuPageFieldTemplateCache> pageByFieldType,
            AuthorizationService authorizationService,
            FilterService filterService,
            ContentProcessorService contentProcessorService,
            FilterAggregator filterAggregator, 
            RouteRequestInfoAccessor routeRequestInfoAccessor)
        {
            _requestModelAccessor = requestModelAccessor;
            _categoryService = categoryService;
            _pageService = pageService;
            _urlService = urlService;
            _pageByFieldType = pageByFieldType;
            _authorizationService = authorizationService;
            _filterService = filterService;
            _contentProcessorService = contentProcessorService;
            _filterAggregator = filterAggregator;
            _routeRequestInfoAccessor = routeRequestInfoAccessor;
            _channelSystemId = _requestModelAccessor.RequestModel.ChannelModel.SystemId;
            _websiteSystemId = _requestModelAccessor.RequestModel.WebsiteModel.SystemId;
        }

        /// <summary>
        /// Builds model
        /// </summary>
        /// <returns></returns>
        public NavigationViewModel Build()
        {
            var model = new NavigationViewModel();
            var megaMenuPages = GetMegaMenuPages();
            foreach (var megaMenuPage in megaMenuPages)
            {
                var linkName = megaMenuPage?.Page?.Localizations.CurrentCulture.Name;
                if (string.IsNullOrEmpty(linkName))
                {
                    continue;
                }
                var contentLinkModel = new ContentLinkModel
                {
                    Name = linkName
                };
                var categoryModel = megaMenuPage.Fields.GetValue<Guid?>(MegaMenuPageFieldNameConstants.MegaMenuCategory)?.MapTo<CategoryModel>();
                PointerPageItem pointerPageItem = null;
                // If a category is chosen, the link will redirect to the category in the first place
                if (categoryModel != null)
                {
                    if (categoryModel.Category == null || !categoryModel.Category.IsPublished(_channelSystemId))
                    {
                       //Category must be published
                        continue;
                    }
                    contentLinkModel.Url = _urlService.GetUrl(categoryModel.Category);
                    contentLinkModel.IsSelected = _selectedStructureId.Contains(categoryModel.Category.SystemId);
                }
                else
                {
                    pointerPageItem = megaMenuPage.Fields.GetValue<PointerPageItem>(MegaMenuPageFieldNameConstants.MegaMenuPage);
                    var linkModel = pointerPageItem?.MapTo<LinkModel>();
                    if (linkModel == null)
                    { 
                        //Page must be active
                        continue;
                    }
                    
                    contentLinkModel.Url = linkModel.Href;
                    contentLinkModel.IsSelected = _selectedStructureId.Contains(pointerPageItem.EntitySystemId);
                }

                model.ContentLinks.Add(contentLinkModel);

                if (megaMenuPage.Fields.GetValue<bool>(MegaMenuPageFieldNameConstants.MegaMenuShowContent))
                {
                    if (megaMenuPage.Fields.GetValue<bool>(MegaMenuPageFieldNameConstants.MegaMenuShowSubCategories))
                    {
                        // Show two levels of sub categories/pages
                        contentLinkModel.Links = categoryModel != null ? GetSubCategoryLinks(categoryModel, true) : GetSubPageLinks(pointerPageItem, true);
                    }
                    else
                    {
                        // Render mega menu context
                        contentLinkModel.Links = GetMegaMenuContent(megaMenuPage, categoryModel);
                    }
                }
                if (!contentLinkModel.IsSelected)
                {
                    contentLinkModel.IsSelected = IsChildSelected(contentLinkModel);
                }
            }

            var selectedCount = model.ContentLinks.Count(x => x.IsSelected);
            if (selectedCount <= 1)
            {
                return model;
            }
            
            var selectedPagesToClear = model.ContentLinks.Where(x => x.IsSelected).Take(selectedCount - 1);
            foreach (var page in selectedPagesToClear)
            {
                UnSelect(page);
            }
            return model;
        }

        private List<ContentLinkModel> GetMegaMenuContent(PageModel pageModel, CategoryModel categoryModel)
        {
            var megaMenuContentLinks = new List<ContentLinkModel>();
            var megaMenus = pageModel.Fields.GetValue<IList<MultiFieldItem>>(MegaMenuPageFieldNameConstants.MegaMenuColumn);
            if (megaMenus != null)
            {
                foreach (var megaMenu in megaMenus)
                {
                    var contentLink = new ContentLinkModel
                    {
                        Name = megaMenu.Fields.GetValue<string>(MegaMenuPageFieldNameConstants.MegaMenuColumnHeader, CultureInfo.CurrentUICulture)
                    };

                    var links = new List<ContentLinkModel>();
                    // Categories has higher priority
                    var categories = megaMenu.Fields.GetValue<IList<PointerItem>>(MegaMenuPageFieldNameConstants.MegaMenuCategories)?.Select(x => x.EntitySystemId.MapTo<CategoryModel>()).Where(x => x != null && x.Category.IsPublished(_channelSystemId));
                    if (categories != null)
                    {
                        links.AddRange(categories.Select(category => new ContentLinkModel()
                        {
                            Name = category.Category.Localizations.CurrentCulture.Name,
                            Url = _urlService.GetUrl(category.Category),
                            IsSelected = _selectedStructureId.Contains(category.Category.SystemId)
                        }));
                    }
                    else if (megaMenu.Fields.GetValue<IList<PointerItem>>(MegaMenuPageFieldNameConstants.MegaMenuPages) != null)
                    {
                        var pages = megaMenu.Fields.GetValue<IList<PointerItem>>(MegaMenuPageFieldNameConstants.MegaMenuPages).OfType<PointerPageItem>().ToList().Select(x => new Tuple<Guid, LinkModel>(x.EntitySystemId, x.MapTo<LinkModel>())).Where(x => x.Item2?.Href != null);

                        links.AddRange(pages.Select(contentPageModel => new ContentLinkModel()
                        {
                            Name = contentPageModel.Item2.Text,
                            Url = contentPageModel.Item2.Href,
                            IsSelected = _selectedStructureId.Contains(contentPageModel.Item1)
                        }));
                    }
                    // Filters works just with category
                    else if (categoryModel != null && megaMenu.Fields.GetValue<IList<string>>(MegaMenuPageFieldNameConstants.MegaMenuFilters) != null)
                    {
                        links.AddRange(GetFilters(categoryModel, megaMenu.Fields.GetValue<IList<string>>(MegaMenuPageFieldNameConstants.MegaMenuFilters).ToList()));
                    }
                    else if (!string.IsNullOrEmpty(megaMenu.Fields.GetValue<string>(MegaMenuPageFieldNameConstants.MegaMenuEditor)))
                    {
                        links.Add(new ContentLinkModel()
                        {
                            Name = _contentProcessorService.Process(megaMenu.Fields.GetValue<string>(MegaMenuPageFieldNameConstants.MegaMenuEditor))
                        });
                    }

                    if (!string.IsNullOrEmpty(megaMenu.Fields.GetValue<string>(MegaMenuPageFieldNameConstants.MegaMenuAdditionalLink)))
                    {
                        var link = string.Empty;
                        var linkToCategoryModel = megaMenu.Fields.GetValue<Guid?>(MegaMenuPageFieldNameConstants.MegaMenuLinkToCategory)?.MapTo<CategoryModel>();
                        if (linkToCategoryModel != null)
                        {
                            if (linkToCategoryModel.Category != null && linkToCategoryModel.Category.IsPublished(_channelSystemId))
                                link = _urlService.GetUrl(linkToCategoryModel.Category);
                        }
                        else
                        {
                            var linkedPageModel = megaMenu.Fields.GetValue<PointerPageItem>(MegaMenuPageFieldNameConstants.MegaMenuLinkToPage)?.MapTo<LinkModel>();
                            if (linkedPageModel != null)
                            {
                                link = linkedPageModel.Href;
                            }
                        }
                        if (!string.IsNullOrEmpty(link))
                        {
                            links.Add(new ContentLinkModel()
                            {
                                Attributes = new Dictionary<string, string>
                            {
                                {
                                    "cssValue", "nav-link"
                                }
                            },
                                Name = megaMenu.Fields.GetValue<string>(MegaMenuPageFieldNameConstants.MegaMenuAdditionalLink),
                                Url = link
                            });
                        }
                    }
                    contentLink.Links = links;
                    megaMenuContentLinks.Add(contentLink);
                }
            }

            return megaMenuContentLinks;
        }

        private List<PageModel> GetMegaMenuPages()
        {
            var megaMenuPages = new List<PageModel>();
            _pageByFieldType.TryFindPage(page =>
           {
               if (page != null)
               {
                   var pageModel = page.MapTo<PageModel>();
                   // pageModel can be null if current user does not have access to it
                   // or if the page is not published on the current channel
                   if (pageModel != null)
                   {
                       megaMenuPages.Add(pageModel);
                   }
               }

               return false;
           });
            return megaMenuPages.OrderBy(p => p.Page.SortIndex).ToList();
        }

        private List<ContentLinkModel> GetSubPageLinks(PointerPageItem pointerPageItem, bool showNextLevel)
        {
            var links = new List<ContentLinkModel>();
            foreach (var subPage in _pageService.GetChildPages(pointerPageItem.EntitySystemId).Select(x=> new PointerPageItem{EntitySystemId = x.SystemId, ChannelSystemId = pointerPageItem.ChannelSystemId})
                .Select(x => new Tuple<PointerPageItem, LinkModel>(x, x.MapTo<LinkModel>())).Where(x => x.Item2?.Href != null))
            {
                var subLinkModel = new ContentLinkModel
                {
                    Name = subPage.Item2.Text,
                    Url = subPage.Item2.Href,
                    IsSelected = _selectedStructureId.Contains(subPage.Item1.EntitySystemId)
                };
                if (showNextLevel)
                {
                    subLinkModel.Links = GetSubPageLinks(subPage.Item1, false);
                }
                links.Add(subLinkModel);
            }

            return links;
        }

        private List<ContentLinkModel> GetSubCategoryLinks(CategoryModel categoryModel, bool showNextLevel)
        {
            var links = new List<ContentLinkModel>();
            foreach (var subCategory in _categoryService.GetChildCategories(categoryModel.Category.SystemId, Guid.Empty)
                                                        .Where(x=>x.IsPublished(_channelSystemId) && _authorizationService.HasOperation<Category>(Operations.Entity.Read, x.SystemId))
                                                        .Select(x => x.MapTo<CategoryModel>()))
            {
                var subLinkModel = new ContentLinkModel
                {
                    Name = subCategory.Category.Localizations.CurrentCulture.Name,
                    IsSelected = _selectedStructureId.Contains(subCategory.Category.SystemId),
                    Url = _urlService.GetUrl(subCategory.Category)
                };
                if (showNextLevel)
                {
                    subLinkModel.Links = GetSubCategoryLinks(subCategory, false);
                }
                links.Add(subLinkModel);
            }

            return links;
        }

        private bool IsChildSelected(ContentLinkModel link)
        {
            var anySelected = link.IsSelected;
            if (link.Links == null)
            {
                return anySelected;
            }
            foreach (var child in link.Links)
            {
                if (IsChildSelected(child))
                {
                    anySelected = child.IsSelected = true;
                }
            }

            return anySelected;
        }

        private void UnSelect(ContentLinkModel link)
        {
            link.IsSelected = false;
            if (link.Links == null)
            {
                return;
            }
            
            foreach (var child in link.Links)
            {
                UnSelect(child);
            }
        }

        private List<ContentLinkModel> GetFilters(CategoryModel categoryModel, List<string> selectedFilters)
        {
            var result = new List<ContentLinkModel>();
            var allFilterFields = _filterService.GetProductFilteringFields();
            var fields = selectedFilters.Where(x => allFilterFields.Contains(x)).ToList();
            var categoryShowRecursively = _requestModelAccessor.RequestModel.WebsiteModel.GetNavigationType() == NavigationType.Filter;

            var searchQuery = new SearchQuery
            {
                CategorySystemId = categoryModel.SystemId,
                CategoryShowRecursively = categoryShowRecursively
            };
            foreach (var filterGroup in _filterAggregator.GetFilter(searchQuery, fields))
            {
                result.Add(filterGroup.MapTo<ContentLinkModel>());
            }
            return result;
        }
    }
}
