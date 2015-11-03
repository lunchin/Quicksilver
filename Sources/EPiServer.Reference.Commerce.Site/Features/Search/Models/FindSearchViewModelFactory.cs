using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using BrilliantCut.Core.Extensions;
using BrilliantCut.Core.Models;
using BrilliantCut.Core.Service;
using EPiServer.Cms.Shell.UI.Rest.ContentQuery;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Find.Framework;
using EPiServer.Framework.Localization;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.Reference.Commerce.Site.Features.Search.Services;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Services.Rest;
using Mediachase.Commerce;
using Mediachase.Search.Extensions;

namespace EPiServer.Reference.Commerce.Site.Features.Search.Models
{
    public class FindSearchViewModelFactory : SearchViewModelFactory
    {
        private readonly FacetService _facetService;
        private readonly ContentFilterService _contentFilterService;

        public FindSearchViewModelFactory(LocalizationService localizationService, ISearchService searchService, FacetService facetService, ContentFilterService contentFilterService) 
            : base(localizationService, searchService)
        {
            _facetService = facetService;
            _contentFilterService = contentFilterService;
        }

        public override SearchViewModel<T> Create<T>(T currentContent, FilterOptionFormModel formModel)
        {
            var parameters = GetParameters(currentContent.ContentLink, formModel);
            var searchFacetGroups = GetFacets(parameters, formModel);

            var facetContents = GetFacetContent(parameters).ToArray();
            var productViewModels = GetProductViewModels(facetContents);
            var associations = GetAssociations(facetContents);

            formModel.TotalCount = parameters.Range.Total.GetValueOrDefault();
            formModel.FacetGroups = GetFacetGroupOptions(searchFacetGroups);

            formModel.Sorting = new[] 
            {
                new SelectListItem()
                {
                    Text = "Name",
                    Value = "Name",
                    Selected = true
                }
            };

            return new SearchViewModel<T>
            {
                CurrentContent = currentContent,
                ProductViewModels = productViewModels,
                Facets = searchFacetGroups.ToArray(),
                FormModel = formModel,
                Associations = associations
            };
        }

        private IEnumerable<ProductViewModel> GetAssociations(IEnumerable<IFacetContent> facetContents)
        {
            var contentTypeIdWithMostHits = facetContents
                .GroupBy(x => x.ContentTypeID)
                .Select(x => new { x.Key, Count = x.Count() })
                .OrderBy(x => x.Count)
                .Select(x => x.Key)
                .FirstOrDefault();

            if (contentTypeIdWithMostHits != 0)
            {
                var productTypeWithMostHits = ServiceLocator.Current.GetInstance<IContentTypeRepository>().Load(contentTypeIdWithMostHits).ModelType;

                var relatedQuery = SearchClient.Instance.Search<FashionProduct>();

                var distinctContentTypes = facetContents.Select(x => x.ContentTypeID).Distinct();
                foreach (var contentType in distinctContentTypes)
                {
                    relatedQuery = relatedQuery.Filter(x => !x.ContentTypeID.Match(contentType));
                }

                var contentItems = ServiceLocator.Current.GetInstance<IContentLoader>().GetItems(facetContents.Where(x => x.ContentTypeID == contentTypeIdWithMostHits).Select(x => x.ContentLink), CultureInfo.InvariantCulture);
                if (typeof(IDegree).IsAssignableFrom(productTypeWithMostHits))
                {
                    var degreeItems = contentItems.OfType<IDegree>().ToArray();
                    if (degreeItems.Any())
                    {
                        var filterBuilder = new FilterBuilder<IDegree>(relatedQuery.Client);
                        filterBuilder =
                            filterBuilder.And(
                                x => x.Degree.InRange(degreeItems.Min(y => y.Degree), degreeItems.Max(y => y.Degree)));

                        relatedQuery = relatedQuery.Filter(filterBuilder);
                    }
                }
                else if (typeof(ISeason).IsAssignableFrom(productTypeWithMostHits))
                {
                    var seasonItems = contentItems.OfType<ISeason>().ToArray();
                    if (seasonItems.Any())
                    {

                        var filterBuilder = new FilterBuilder<ISeason>(relatedQuery.Client);
                        foreach (var seasonItem in seasonItems.Select(x => x.Season).Distinct())
                        {
                            filterBuilder = filterBuilder.Or(x => x.Season.Match(seasonItem));
                        }
                        
                        relatedQuery = relatedQuery.Filter(filterBuilder);
                    }
                }

                var associatedFacetContents = relatedQuery
                    .FilterForVisitor()
                    .Select(x => new FacetContent
                    {
                        Name = x.Name,
                        ContentGuid = x.ContentGuid,
                        ContentLink = x.ContentLink,
                        DefaultPriceValue = x.DefaultPriceValue(),
                        ThumbnailPath = x.ThumbnailUrl(),
                        LinkUrl = x.LinkUrl(),
                        DefaultImageUrl = x.DefaultImageUrl()
                    })
                    .Take(5)
                    .GetResult();

                return GetProductViewModels(associatedFacetContents);
            }

            return Enumerable.Empty<ProductViewModel>();
        }

        private ContentQueryParameters GetParameters(ContentReference contentLink, FilterOptionFormModel formModel)
        {
            var page = formModel.Page > 0 ? formModel.Page - 1 : 0;
            var pageSize = formModel.PageSize > 0 ? formModel.PageSize : 100;

            var parameters = new ContentQueryParameters
            {
                ReferenceId = contentLink,
                Range = new ItemRange
                {
                    Start = page * pageSize,
                    End = (page * pageSize) + pageSize
                },
                SortColumns = new[]
                {
                    new SortColumn
                    {
                        ColumnName = formModel.Sort ?? "Name"
                    }
                },
                AllParameters = new NameValueCollection
                {
                    {"filterModel", GetCheckedOptionString(formModel)},
                    {"listingMode", "1"},
                    {"productGrouped", "false"},
                    {"searchType",typeof(ProductContent).AssemblyQualifiedName}
                }
            };
             
            return parameters;
        }

        private IEnumerable<ProductViewModel> GetProductViewModels(IEnumerable<IFacetContent> facetContent)
        {
            var productViewModels = new List<ProductViewModel>();
            foreach (IFacetContent x in facetContent)
            {
                AddProductViewModel(x, productViewModels);
            }

            return productViewModels;
        }

        private IEnumerable<IFacetContent> GetFacetContent(ContentQueryParameters parameters)
        {
            return _contentFilterService.GetItems(parameters).OfType<IFacetContent>();
        }

        private static void AddProductViewModel(IFacetContent x, List<ProductViewModel> productViewModels)
        {
            var currency = !String.IsNullOrWhiteSpace(x.DefaultCurrency) ? x.DefaultCurrency : Currency.USD.ToString();

            var amount = x.DefaultPriceValue.GetValueOrDefault();
            var price = new Money(Convert.ToDecimal(amount), currency);

            if (price.Amount <= 0 && x.Prices != null)
            {
                var randomPrice = x.Prices.FirstOrDefault();
                if (randomPrice != null)
                {
                    price = new Money(randomPrice.UnitPrice.Amount, currency);
                }
            }

            
            var categoryName = x.CategoryNames != null ? x.CategoryNames.FirstOrDefault() : string.Empty;

            productViewModels.Add(new ProductViewModel
            {
                DisplayName = x.Name,
                Code = x.Code ?? x.Name,
                ExtendedPrice = price,
                ImageUrl = x.DefaultImageUrl,
                PlacedPrice = price,
                Url = x.LinkUrl,
                Brand = categoryName
            });
        }

        private List<FacetGroupOption> GetFacetGroupOptions(List<FacetGroup> facetGroups)
        {
            var facetGroupOptions = new List<FacetGroupOption>();

            foreach (var facetGroup in facetGroups)
            {
                var option = new FacetGroupOption
                {
                    GroupName = facetGroup.Name,
                    GroupFieldName = facetGroup.FieldName,
                    Facets = new List<FacetOption>()
                };

                foreach (var facet in facetGroup.Facets.OfType<Facet>())
                {
                    option.Facets.Add(new FacetOption() { Name = facet.Name, Key = facet.Key, Count = facet.Count, Selected = facet.IsSelected});
                }

                facetGroupOptions.Add(option);
            }

            return facetGroupOptions;
        }

        private List<FacetGroup> GetFacets(ContentQueryParameters parameters, FilterOptionFormModel formModel)
        {
            var searchFacetGroups = new List<FacetGroup>();
            var filterContentWithOptions = _facetService.GetItems(parameters);
            foreach (var filterContentWithOptionse in filterContentWithOptions)
            {
                var facetGroup = new FacetGroup(filterContentWithOptionse.Name, filterContentWithOptionse.Name);

                foreach (var filterOptionModel in filterContentWithOptionse.FilterOptions)
                {
                    var isSelected = formModel.RoutedFacets != null && formModel.RoutedFacets.ContainsKey(filterContentWithOptionse.Name) && formModel.RoutedFacets[filterContentWithOptionse.Name].Contains(filterOptionModel.Value.ToString());
                    facetGroup.Facets.Add(new Facet(facetGroup, filterContentWithOptionse.Name + "/" + filterOptionModel.Value, filterOptionModel.Value.ToString(), filterOptionModel.Count, isSelected));
                }

                searchFacetGroups.Add(facetGroup);
            }

            return searchFacetGroups;
        }

        private string GetCheckedOptionString(FilterOptionFormModel formModel)
        {
            var stringBuilder = new StringBuilder();

            if (formModel.RoutedFacets == null)
            {
                foreach (var facetGroupOption in formModel.FacetGroups)
                {
                    if (!facetGroupOption.Facets.Any(x => x.Selected))
                    {
                        continue;
                    }

                    stringBuilder.Append(facetGroupOption.GroupFieldName);
                    stringBuilder.Append("__");

                    foreach (var facet in facetGroupOption.Facets.Where(x => x.Selected))
                    {
                        stringBuilder.Append("..");
                        stringBuilder.Append(facet.Key);
                    }

                    stringBuilder.Append("__");
                }

                return stringBuilder.ToString();
            }

            foreach (var routedFacet in formModel.RoutedFacets)
            {
                stringBuilder.Append(routedFacet.Key);
                stringBuilder.Append("__");

                foreach (var facet in routedFacet.Value)
                {
                    stringBuilder.Append("..");
                    stringBuilder.Append(facet);
                }

                stringBuilder.Append("__");
            }

            return stringBuilder.ToString();
        }
    }
}