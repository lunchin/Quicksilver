using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Reference.Commerce.Site.Features.Market.Services;
using EPiServer.Reference.Commerce.Site.Features.Product.Services;
using EPiServer.Reference.Commerce.Site.Features.Product.ViewModels;
using EPiServer.Reference.Commerce.Site.Features.Recommendations.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EPiServer.Reference.Commerce.Site.Features.Recommendations.Controllers
{
    public class RecommendationsController : Controller
    {
        private readonly IContentLoader _contentLoader;
        private readonly LanguageService _languageService;
        private readonly IProductService _productService;

        public RecommendationsController(
            IProductService productService,
            IContentLoader contentLoader, 
            LanguageService languageService)
        {
            _productService = productService;
            _contentLoader = contentLoader;
            _languageService = languageService;
        }

        [ChildActionOnly]
        public ActionResult Index(IEnumerable<ContentReference> entryLinks)
        {
            if (!entryLinks.Any())
            {
                return new EmptyResult();
            }

            var viewModel = new RecommendationsViewModel
            {
                Products = GetProducts(entryLinks)
            };

            return PartialView("_Recommendations", viewModel);
        }

        private IEnumerable<ProductViewModel> GetProducts(IEnumerable<ContentReference> entryLinks)
        {
            var language = _languageService.GetCurrentLanguage();
            var contentTypes = _contentLoader.GetItems(entryLinks, language);
            return contentTypes
                .Select(x => x is ProductContent ?
                _productService.GetProductViewModel((ProductContent)x) :
                _productService.GetProductViewModel((VariationContent)x));
        }
    }
}