﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Filters;
using EPiServer.Find;
using EPiServer.Find.Api.Facets;
using EPiServer.Find.Commerce;
using EPiServer.Find.Framework;
using EPiServer.Reference.Commerce.Site.Features.Find;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.Reference.Commerce.Site.Features.Shared.Extensions;
using EPiServer.Reference.Commerce.Site.Features.Shared.Services;
using EPiServer.Reference.Commerce.Site.Infrastructure.Facades;
using EPiServer.Web.Mvc;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Pricing;
using EPiServer.Reference.Commerce.Site.Features.Market.Services;
using BrilliantCut.Core.Extensions;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Controllers
{
    public class ProductController : ContentController<FashionProduct>
    {
        private readonly IPromotionService _promotionService;
        private readonly IContentLoader _contentLoader;
        private readonly IPriceService _priceService;
        private readonly ICurrentMarket _currentMarket;
        private readonly ICurrencyService _currencyservice;
        private readonly IRelationRepository _relationRepository;
        private readonly AppContextFacade _appContext;
        private readonly UrlResolver _urlResolver;
        private readonly FilterPublished _filterPublished;
        private readonly CultureInfo _preferredCulture;
        private readonly bool _isInEditMode;
        private readonly AssetUrlResolver _assetUrlResolver;

        public ProductController(
            IPromotionService promotionService,
            IContentLoader contentLoader,
            IPriceService priceService,
            ICurrentMarket currentMarket,
            CurrencyService currencyservice, 
            IRelationRepository relationRepository, 
            AppContextFacade appContext, 
            UrlResolver urlResolver,
            FilterPublished filterPublished,
            Func<CultureInfo> preferredCulture,
            Func<bool> isInEditMode, 
            AssetUrlResolver assetUrlResolver)
        {
            _promotionService = promotionService;
            _contentLoader = contentLoader;
            _priceService = priceService;
            _currentMarket = currentMarket;
            _currencyservice = currencyservice;
            _relationRepository = relationRepository;
            _appContext = appContext;
            _urlResolver = urlResolver;
            _preferredCulture = preferredCulture();
            _isInEditMode = isInEditMode();
            _filterPublished = filterPublished;
            _assetUrlResolver = assetUrlResolver;
        }

        [HttpGet]
        public ActionResult Index(FashionProduct currentContent, string variationCode = "", bool quickview = false)
        {
            var variations = GetVariations(currentContent).ToList();
            if (_isInEditMode && !variations.Any())
            {
                var productWithoutVariation = new FashionProductViewModel
                {
                    Product = currentContent,
                    Images = currentContent.GetAssets<IContentImage>(_contentLoader, _urlResolver)
                };
                return Request.IsAjaxRequest() ? PartialView("ProductWithoutVariation", productWithoutVariation) : (ActionResult)View("ProductWithoutVariation", productWithoutVariation);
            }
            FashionVariant variation;
            if (!TryGetFashionVariant(variations, variationCode, out variation))
            {
                return HttpNotFound();
            }

            var market = _currentMarket.GetCurrentMarket();
            var currency = _currencyservice.GetCurrentCurrency();

            var defaultPrice = GetDefaultPrice(variation, market, currency);
            var discountPrice = GetDiscountPrice(defaultPrice, market, currency);

            var viewModel = new FashionProductViewModel
            {
                Product = currentContent,
                Variation = variation,
                OriginalPrice = defaultPrice != null ? defaultPrice.UnitPrice : new Money(0, currency),
                Price = discountPrice,
                Colors = variations
                    .Where(x => x.Size != null && x.Size == variation.Size)
                    .Select(x => new SelectListItem
                    {
                        Selected = false,
                        Text = x.Color,
                        Value = x.Color
                    })
                    .ToList(),
                Sizes = variations
                    .Where(x => x.Color != null && x.Color == variation.Color)
                    .Select(x => new SelectListItem
                    {
                        Selected = false,
                        Text = x.Size,
                        Value = x.Size
                    })
                    .ToList(),
                Color = variation.Color,
                Size = variation.Size,
                Images = variation.GetAssets<IContentImage>(_contentLoader, _urlResolver),
                Assosiations = GetAssociations(currentContent.ContentLink)
            };

            if (quickview)
            {
                return PartialView("Quickview", viewModel);
            }

            return Request.IsAjaxRequest() ? PartialView(viewModel) : (ActionResult)View(viewModel);
        }

        private IEnumerable<ProductViewModel> GetAssociations(ContentReference currentContentLink)
        {
            var contentLinkStrinksCollection = SearchClient.Instance.Search<ContentLinkOrders>()
                .Filter(x => x.ContentLinkStrings.Match(currentContentLink.ToString()))
                .Select(x => x.ContentLinkStrings)
                .GetResult();

            var hashSet = new HashSet<string>();
            foreach (var contentLinkStrinks in contentLinkStrinksCollection)
            {
                foreach (var contentLinkStrink in contentLinkStrinks)
                {
                    if (contentLinkStrink != currentContentLink.ToString())
                    {
                        hashSet.Add(contentLinkStrink);
                    }
                }
            }

            var associationLinks = hashSet.Select(x => new ContentReference(x));
            var associations = _contentLoader.GetItems(associationLinks, CultureInfo.InvariantCulture).OfType<ProductContent>();
            return associations.Select(x => new ProductViewModel()
            {
                DisplayName = x.Name,
                Code = x.Code,
                ImageUrl = _urlResolver.GetUrl(_assetUrlResolver.GetAssetUrl<ImageData>(x)),
                ExtendedPrice = new Money(0, Currency.USD)
            });
        }

        [HttpPost]
        public ActionResult SelectVariant(FashionProduct currentContent, string color, string size)
        {
            var variations = GetVariations(currentContent);

            FashionVariant variation;
            if (!TryGetFashionVariantByColorAndSize(variations, color, size, out variation))
            {
                return HttpNotFound();
            }

            return RedirectToAction("Index", new { variationCode = variation.Code });
        }

        private IEnumerable<FashionVariant> GetVariations(FashionProduct currentContent)
        {
            return _contentLoader
                .GetItems(currentContent.GetVariants(_relationRepository), _preferredCulture)
                .Cast<FashionVariant>()
                .Where(v => v.IsAvailableInCurrentMarket(_currentMarket) && !_filterPublished.ShouldFilter(v));
        }

        private static bool TryGetFashionVariant(IEnumerable<FashionVariant> variations, string variationCode, out FashionVariant variation)
        {
            variation = !string.IsNullOrEmpty(variationCode) ? 
                variations.FirstOrDefault(x => x.Code == variationCode) : 
                variations.FirstOrDefault();

            return variation != null;
        }

        private static bool TryGetFashionVariantByColorAndSize(IEnumerable<FashionVariant> variations, string color, string size, out FashionVariant variation)
        {
            variation = variations.FirstOrDefault(x =>
                x.Color.Equals(color, StringComparison.OrdinalIgnoreCase) &&
                x.Size.Equals(size, StringComparison.OrdinalIgnoreCase));

            return variation != null;
        }

        private IPriceValue GetDefaultPrice(FashionVariant variation, IMarket market, Currency currency)
        {
            return _priceService.GetDefaultPrice(
                market.MarketId,
                DateTime.Now,
                new CatalogKey(_appContext.ApplicationId, variation.Code),
                currency);
        }

        private Money GetDiscountPrice(IPriceValue defaultPrice, IMarket market, Currency currency)
        {
            if (defaultPrice == null)
            {
                return new Money(0, currency);
            }

            return _promotionService.GetDiscountPrice(defaultPrice.CatalogKey, market.MarketId, currency).UnitPrice;
        }
    }
}