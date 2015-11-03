using System;
using System.Collections.Generic;
using BrilliantCut.Core;
using BrilliantCut.Core.Filters;
using BrilliantCut.Core.FilterSettings;
using BrilliantCut.Core.Models;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Framework;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;

namespace EPiServer.Reference.Commerce.Site.Features.Find.Filters
{
    public class DegreeFilter : FilterContentBase<WinterJacketProduct, string>
    {
        public override string Name
        {
            get { return "Temperature"; }
        }

        public override ITypeSearch<WinterJacketProduct> Filter(IContent currentCntent, ITypeSearch<WinterJacketProduct> query, IEnumerable<string> values)
        {
            var filter = SearchClient.Instance.BuildFilter<WinterJacketProduct>();
            foreach (var value in values)
            {
                var valueSplit = value.Split(' ');
                var fromValue = Int32.Parse(valueSplit[0]);
                var toValue = Int32.Parse(valueSplit[2]);
                filter = filter.Or(x => x.Degree.InRange(fromValue, toValue));
            }

            return query.Filter(filter);
        }

        public override IEnumerable<IFilterOptionModel> GetFilterOptions(SearchResults<IFacetContent> searchResults, ListingMode mode, IContent content)
        {
            return new[]
            {
                new FilterOptionModel("-30", "-30 To -25", "-30 To -25", 0, 0),
                new FilterOptionModel("-25", "-25 To -20", "-25 To -20", 0, 0),
                new FilterOptionModel("-20", "-20 To -15", "-20 To -15", 0, 0),
                new FilterOptionModel("-15", "-15 To -10", "-15 To -10", 0, 0),
                new FilterOptionModel("-10", "-10 To -5", "-10 To -5", 0, 0),
                new FilterOptionModel("-5", "-5 To 0", "-5 To 0", 0, 0)
            };
        }

        public override ITypeSearch<WinterJacketProduct> AddfacetToQuery(ITypeSearch<WinterJacketProduct> query, FacetFilterSetting setting)
        {
            return query;
            //return query.TermsFacetFor(x => x.Colors(), request =>
            //{
            //    if (setting.MaxFacetHits.HasValue)
            //    {
            //        request.Size = setting.MaxFacetHits;
            //    }
            //});
        }
    }
}