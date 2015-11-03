//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using EPiServer.ChangeLog;
//using EPiServer.Commerce.Catalog.ContentTypes;
//using EPiServer.DataAbstraction;
//using EPiServer.ServiceLocation;
//using EPiServer.Shell;

//namespace EPiServer.Reference.Commerce.Site.Features.Find.UIDescriptors.ViewConfigurations
//{
//    [ServiceConfiguration(typeof(ViewConfiguration))]
//    public class FilterCatalogContentListView : ViewConfiguration<NodeContentBase>
//    {
//        /// <summary>
//        /// Initializes a new instance of the <see cref="FilterCatalogContentListView"/> class.
//        /// </summary>
//        public FilterCatalogContentListView()
//        {
//            Key = "catalogcontentlist";
//            LanguagePath = "/commerce/contentediting/views/cataloglist";
//            ControllerType = "quicksilver/widget/FilterCatalogContentlist";
//            IconClass = "epi-iconList";
//            SortOrder = 10;
//            Category = "catalog";
//        }
//    }
//}