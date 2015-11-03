using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace EPiServer.Reference.Commerce.Site.Features.Find
{
    public class AssetUrlResolverWithFallback : AssetUrlResolver
    {
        private readonly IContentLoader _contentLoader;

        public AssetUrlResolverWithFallback(UrlResolver urlResolver, AssetUrlConventions assetUrlConvention, IContentLoader contentLoader) 
            : base(urlResolver, assetUrlConvention, contentLoader)
        {
            _contentLoader = contentLoader;
        }

        //public override string GetAssetUrl<TContentMedia>(ItemCollection<CommerceMedia> mediaList, string defaultGroup)
        //{
        //    if (!mediaList.Any())
        //    {
        //        _contentLoader.Get<IContent>()
        //    }

        //    return base.GetAssetUrl<TContentMedia>(mediaList, defaultGroup);
        //}

        public override string GetAssetUrl<TContentMedia>(IAssetContainer assetContainer, Func<CommerceMedia, string> resolveForSelected)
        {
            if (!assetContainer.CommerceMediaCollection.Any())
            {
                var content = assetContainer as IContent;
                if (content != null)
                {
                    var parent = _contentLoader.Get<IContent>(content.ParentLink) as IAssetContainer;
                    if (parent != null)
                    {
                        return base.GetAssetUrl<TContentMedia>(parent, resolveForSelected);
                    }
                }
            }

            return base.GetAssetUrl<TContentMedia>(assetContainer, resolveForSelected);
        }
    }
}