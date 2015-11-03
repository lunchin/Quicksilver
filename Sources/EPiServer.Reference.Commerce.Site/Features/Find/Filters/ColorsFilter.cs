using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BrilliantCut.Core;
using BrilliantCut.Core.Filters;
using BrilliantCut.Core.FilterSettings;
using BrilliantCut.Core.Models;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Framework;
using EPiServer.Reference.Commerce.Site.Extensions;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;

namespace EPiServer.Reference.Commerce.Site.Features.Find.Filters
{
    public class ColorsFilter : FilterContentBase<FashionProduct, string>
    {
        public override string Name
        {
            get { return "Fashion colors"; }
        }

        public override ITypeSearch<FashionProduct> Filter(IContent currentCntent, ITypeSearch<FashionProduct> query, IEnumerable<string> values)
        {
            var filter = SearchClient.Instance.BuildFilter<FashionProduct>();
            filter = values.Aggregate(filter, (current, value) => current.Or(x => x.Colors().MatchCaseInsensitive(value)));

            return query.Filter(filter);
        }

        public override IEnumerable<IFilterOptionModel> GetFilterOptions(SearchResults<IFacetContent> searchResults, ListingMode mode, IContent content)
        {
            var facet = searchResults
                .TermsFacetFor<FashionProduct>(x => x.Colors()).Terms;

            return facet.Select(authorCount => new FilterOptionModel("fashion_colors" + authorCount.Term, String.Format(CultureInfo.InvariantCulture, "{0} ({1})", authorCount.Term, authorCount.Count), authorCount.Term, false, authorCount.Count));
        }

        public override ITypeSearch<FashionProduct> AddfacetToQuery(ITypeSearch<FashionProduct> query, FacetFilterSetting setting)
        {
            return query.TermsFacetFor(x => x.Colors(), request =>
            {
                if (setting.MaxFacetHits.HasValue)
                {
                    request.Size = setting.MaxFacetHits;
                }
            });
        }
    }
}