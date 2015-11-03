using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;
using BrilliantCut.Core;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Routing;
using EPiServer.Core;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Segments;
using StructureMap;

namespace EPiServer.Reference.Commerce.Site.Features.Find
{
    public class FacetPartialRoute : HierarchicalCatalogPartialRouter
    {
        private readonly FilterConfiguration _filterConfiguration;

        public FacetPartialRoute(Func<ContentReference> routeStartingPoint, CatalogContentBase commerceRoot, bool enableOutgoingSeoUri, FilterConfiguration filterConfiguration) 
            : base(routeStartingPoint, commerceRoot, enableOutgoingSeoUri)
        {
            _filterConfiguration = filterConfiguration;
        }

        [DefaultConstructor]
        public FacetPartialRoute(Func<ContentReference> routeStartingPoint, CatalogContentBase commerceRoot, bool supportSeoUri, IContentLoader contentLoader, IRoutingSegmentLoader routingSegmentLoader, IContentVersionRepository contentVersionRepository, IUrlSegmentRouter urlSegmentRouter, IContentLanguageSettingsHandler contentLanguageSettingsHandler, FilterConfiguration filterConfiguration) 
            : base(routeStartingPoint, commerceRoot, supportSeoUri, contentLoader, routingSegmentLoader, contentVersionRepository, urlSegmentRouter, contentLanguageSettingsHandler)
        {
            _filterConfiguration = filterConfiguration;
        }

        public override object RoutePartial(PageData content, SegmentContext segmentContext)
        {
            var routedContet = base.RoutePartial(content, segmentContext);
            var filterNames = _filterConfiguration.Filters.Select(x => x.Key.Name).ToArray();
            
            var segmentPair = segmentContext.GetNextValue(segmentContext.RemainingPath);
            if (String.IsNullOrEmpty(segmentPair.Next) || !filterNames.Contains(segmentPair.Next))
            {
                return routedContet;
            }

            segmentContext.RouteData.DataTokens.Add("facets", new Dictionary<string, List<object>>());
            AddFacetsToSegmentContext(segmentContext, segmentPair, filterNames, null);
            return routedContet;
        }

        private static void AddFacetsToSegmentContext(SegmentContext segmentContext, SegmentPair segmentPair, string[] filterNames, string currentFacetName)
        {
            var nextSegment = GetNextSegment(segmentPair);
            if (String.IsNullOrEmpty(nextSegment))
            {
                return;
            }

            var facets = segmentContext.RouteData.DataTokens["facets"] as Dictionary<string, List<object>>;
            if (filterNames.Contains(nextSegment))
            {
                currentFacetName = nextSegment;
                facets[currentFacetName] = new List<object>();
            }
            else
            {
                facets[currentFacetName].Add(nextSegment);
            }

            segmentContext.RemainingPath = segmentPair.Remaining;

            segmentPair = segmentContext.GetNextValue(segmentContext.RemainingPath);
            AddFacetsToSegmentContext(segmentContext, segmentPair, filterNames, currentFacetName);
        }

        private static string GetNextSegment(SegmentPair segmentPair)
        {
            return segmentPair.Next.Replace('_', ' ');
        }
    }
}