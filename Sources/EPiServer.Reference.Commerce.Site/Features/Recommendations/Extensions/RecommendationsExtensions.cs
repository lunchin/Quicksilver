using EPiServer.Core;
using EPiServer.Recommendations.Tracking.Data;
using Mediachase.Commerce.Catalog;
using System.Collections.Generic;
using System.Linq;

namespace EPiServer.Reference.Commerce.Site.Features.Recommendations.Extensions
{
    public static class RecommendationsExtensions
    {
        private const string ProductAlternatives = "productAlternativesWidget";
        private const string ProductCrossSells = "productCrossSellsWidget";
        private const string Home = "homewidget1";
        private const string Category = "categoryWidget";
        private const string SearchResult = "searchWidget";
         
        public static IEnumerable<ContentReference> GetAlternativeProductsRecommendations(this TrackingResponseData response, ReferenceConverter referenceConverter)
        {
            return response.SmartRecs
                    ?.Where(x => x.Position == ProductAlternatives)
                    .SelectMany(x => x.Recs)
                    .Select(x => referenceConverter.GetContentLink(x.RefCode));
        }

        public static IEnumerable<ContentReference> GetCrossSellProductsRecommendations(this TrackingResponseData response, ReferenceConverter referenceConverter)
        {
            return response.SmartRecs
                    ?.Where(x => x.Position == ProductCrossSells)
                    .SelectMany(x => x.Recs)
                    .Select(x => referenceConverter.GetContentLink(x.RefCode));
        }

        public static IEnumerable<ContentReference> GetHomeRecommendations(this TrackingResponseData response, ReferenceConverter referenceConverter)
        {
            return response.SmartRecs
                    ?.Where(x => x.Position == Home)
                    .SelectMany(x => x.Recs)
                    .Select(x => referenceConverter.GetContentLink(x.RefCode));
        }

        public static IEnumerable<ContentReference> GetCategoryRecommendations(this TrackingResponseData response, ReferenceConverter referenceConverter)
        {
            return response.SmartRecs
                    ?.Where(x => x.Position == Category)
                    .SelectMany(x => x.Recs)
                    .Select(x => referenceConverter.GetContentLink(x.RefCode));
        }

        public static IEnumerable<ContentReference> GetSearchResultRecommendations(this TrackingResponseData response, ReferenceConverter referenceConverter)
        {
            return response.SmartRecs
                    ?.Where(x => x.Position == SearchResult)
                    .SelectMany(x => x.Recs)
                    .Select(x => referenceConverter.GetContentLink(x.RefCode));
        }
    }
}