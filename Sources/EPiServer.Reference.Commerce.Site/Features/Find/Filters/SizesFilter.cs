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
    public class SizesFilter : FilterContentBase<FashionProduct, string>
    {
        public override string Name
        {
            get { return "Fashion Sizes"; }
        }

        public override ITypeSearch<FashionProduct> Filter(IContent currentCntent, ITypeSearch<FashionProduct> query, IEnumerable<string> values)
        {
            var filter = SearchClient.Instance.BuildFilter<FashionProduct>();
            filter = values.Aggregate(filter, (current, value) => current.Or(x => x.Sizes().MatchCaseInsensitive(value)));

            return query.Filter(filter);
        }

        public override IEnumerable<IFilterOptionModel> GetFilterOptions(SearchResults<IFacetContent> searchResults, ListingMode mode, IContent content)
        {
            var facet = searchResults
                .TermsFacetFor<FashionProduct>(x => x.Sizes()).Terms;

            return facet.Select(authorCount => new FilterOptionModel("fashion_sizes" + authorCount.Term, String.Format(CultureInfo.InvariantCulture, "{0} ({1})", authorCount.Term, authorCount.Count), authorCount.Term, false, authorCount.Count));
        }

        public override ITypeSearch<FashionProduct> AddfacetToQuery(ITypeSearch<FashionProduct> query, FacetFilterSetting setting)
        {
            return query.TermsFacetFor(x => x.Sizes(), request =>
            {
                if (setting.MaxFacetHits.HasValue)
                {
                    request.Size = setting.MaxFacetHits;
                }
            });
        }
    }
}