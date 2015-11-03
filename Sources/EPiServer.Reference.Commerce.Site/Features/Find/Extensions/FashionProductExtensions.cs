using System.Collections.Generic;
using System.Linq;
using BrilliantCut.Core.Extensions;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.ServiceLocation;

namespace EPiServer.Reference.Commerce.Site.Extensions
{
    public static class FashionProductExtensions
    {
        public static IEnumerable<string> Sizes(this FashionProduct content)
        {
            var variationLinks = content.Variations();
            var variations = ServiceLocator.Current.GetInstance<IContentLoader>()
                .GetItems(variationLinks, content.Language).OfType<FashionVariant>();

            return variations.Select(x => x.Size).Distinct().ToArray();
        }

        public static IEnumerable<string> Colors(this FashionProduct content)
        {
            var variationLinks = content.Variations();
            var variations = ServiceLocator.Current.GetInstance<IContentLoader>()
                .GetItems(variationLinks, content.Language).OfType<FashionVariant>();

            return variations.Select(x => x.Color).Distinct().ToArray();
        }
    }
}