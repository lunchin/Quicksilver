using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;
using BrilliantCut.Core;
using BrilliantCut.Core.Filters.Implementations;
using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Routing;
using EPiServer.Core;
using EPiServer.Editor;
using EPiServer.Find;
using EPiServer.Find.ClientConventions;
using EPiServer.Find.Cms;
using EPiServer.Find.Cms.Conventions;
using EPiServer.Find.Framework;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Framework.Web;
using EPiServer.Globalization;
using EPiServer.Reference.Commerce.Shared.Models.Identity;
using EPiServer.Reference.Commerce.Site.Extensions;
using EPiServer.Reference.Commerce.Site.Features.Find;
using EPiServer.Reference.Commerce.Site.Features.Find.Filters;
using EPiServer.Reference.Commerce.Site.Features.Market.Services;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.Reference.Commerce.Site.Features.Search.Models;
using EPiServer.Reference.Commerce.Site.Infrastructure.WebApi;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Security;
using Mediachase.Commerce.Website.Helpers;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using StructureMap.Web;

namespace EPiServer.Reference.Commerce.Site.Infrastructure
{
    [ModuleDependency(typeof(EPiServer.Find.Commerce.FindCommerceInitializationModule))]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class SiteInitialization : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
            //CatalogRouteHelper.MapDefaultHierarchialRouter(RouteTable.Routes, false);
            var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var commerceRootContent = contentLoader.Get<CatalogContentBase>(referenceConverter.GetRootLink());
            RouteTable.Routes.RegisterPartialRouter(new FacetPartialRoute(() => ContentReference.IsNullOrEmpty(SiteDefinition.Current.StartPage) ?
                    SiteDefinition.Current.RootPage :
                    SiteDefinition.Current.StartPage, commerceRootContent, false, context.Locate.Advanced.GetInstance<FilterConfiguration>()));


            GlobalFilters.Filters.Add(new HandleErrorAttribute());

            context.Locate.DisplayChannelService().RegisterDisplayMode(new DefaultDisplayMode(RenderingTags.Mobile)
            {
                ContextCondition = r => r.GetOverriddenBrowser().IsMobileDevice
            });
            
            AreaRegistration.RegisterAllAreas();

            //context.Locate.Advanced.GetInstance<AssetUrlConventions>().DefaultAssetUrl = "";

            //ContentIndexer.Instance.Conventions.ForInstancesOf<JacketProduct>().ShouldIndex(x => false);

            SearchClient.Instance.Conventions.ForInstancesOf<FashionProduct>()
                .IncludeField(x => x.Sizes())
                .IncludeField(x => x.Colors());

            context.Locate.Advanced.GetInstance<FilterConfiguration>()
                .Facet<CategoryFilter>()
                .Termsfacet<JacketProduct>(x => x.Season, (builder, value) => builder.Or(x => x.Season.Match(value)))
                .Termsfacet<FashionProduct>(x => x.Brand, (builder, value) => builder.Or(x => x.Brand.Match(value)))
                .Facet<DegreeFilter>()
                .Facet<SizesFilter>()
                .Facet<ColorsFilter>()
                .Facet<LanguageFilter>()
                .Facet<MarketsFilter>()
                .Facet<ChildrenDescendentsFilter>();

            var assetUrlConventions = context.Locate.Advanced.GetInstance<AssetUrlConventions>();
            assetUrlConventions.AddDefaultGroup<JacketProduct>("other");
            assetUrlConventions.AddDefaultGroup<WinterJacketProduct>("default");

                //.RangeFacet<WinterJacketProduct, double>(x => x.Degree,
                //    (builder, values) => builder
                //        .And(x => x.Degree.GreaterThan((int)values.Min() - 1))
                //        .And(x => x.Degree.LessThan((int)values.Max() + 1)))

                //.Termsfacet<WinterJacketProduct>(x => x.Degree, (builder, value) => builder.Or(x => x.Degree.Match(value)))
                //.Termsfacet<FashionProduct>(x => x.Sizes(), (builder, value) => builder.Or(x => x.Sizes().MatchContained(value)))
                //.Termsfacet<FashionProduct>(x => x.Colors(), (builder, value) => builder.Or(x => x.Colors().MatchContained(value)))
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Container.Configure(c =>
            {
                c.For<ICurrentMarket>().Singleton().Use<CurrentMarket>();

                c.For<Func<string, CartHelper>>()
                .HybridHttpOrThreadLocalScoped()
                .Use(() => new Func<string, CartHelper>((cartName) => new CartHelper(cartName, PrincipalInfo.CurrentPrincipal.GetContactId())));

                //Register for auto injection of edit mode check, should be default life cycle (per request)
                c.For<Func<bool>>()
                .Use(() => new Func<bool>(() => PageEditing.PageIsInEditMode));                 

                c.For<IUpdateCurrentLanguage>()
                    .Singleton()
                    .Use<LanguageService>()
                    .Setter<IUpdateCurrentLanguage>()
                    .Is(x => x.GetInstance<UpdateCurrentLanguage>());

                c.For<Func<CultureInfo>>().Use(() => new Func<CultureInfo>(() => ContentLanguage.PreferredCulture));

                Func<IOwinContext> owinContextFunc = () => HttpContext.Current.GetOwinContext();
                c.For<ApplicationUserManager>().Use(() => owinContextFunc().GetUserManager<ApplicationUserManager>());
                c.For<ApplicationSignInManager>().Use(() => owinContextFunc().Get<ApplicationSignInManager>());
                c.For<IAuthenticationManager>().Use(() => owinContextFunc().Authentication);
                c.For<IOwinContext>().Use(() => owinContextFunc());
                c.For<IModelBinderProvider>().Use<ModelBinderProvider>();
                c.For<SearchViewModelFactory>().Use<FindSearchViewModelFactory>();
                c.For<AssetUrlResolver>().Use<AssetUrlResolverWithFallback>();
            });

            DependencyResolver.SetResolver(new StructureMapDependencyResolver(context.Container));
            GlobalConfiguration.Configure(config =>
            {
                config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.LocalOnly;
                config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings();
                config.Formatters.XmlFormatter.UseXmlSerializer = true;
                config.DependencyResolver = new StructureMapResolver(context.Container);
                config.MapHttpAttributeRoutes();
            });
        }

        public void Uninitialize(InitializationEngine context) { }
    }
}