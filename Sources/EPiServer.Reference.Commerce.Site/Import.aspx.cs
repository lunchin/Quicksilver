using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Find.Framework;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Inventory;
using Mediachase.Commerce.Pricing;

namespace EPiServer.Reference.Commerce.Site
{
    public partial class Import : System.Web.UI.Page
    {
        private readonly IEnumerable<string> _jacketBrands = new string[]
        {
            "ADIDAS ORIGINALS",
            "AJ MORGAN",
            "ALDO",
            "ALPHA INDUSTRIES",
            "AMERICAN APPAREL",
            "ANTIOCH",
            "ANTONY MORATO",
            "ARMANI EXCHANGE",
            "ASICS",
            "ASOS",
            "BARBOUR",
            "BASE LONDON",
            "BELLFIELD",
            "BEN SHERMAN",
            "BILLABONG",
            "BIRKENSTOCK",
            "BJORN BORG",
            "BLACK DUST",
            "BLACK KAVIAR",
            "BLEND",
            "BLUEBEARDS REVENGE",
            "BOARDIES",
            "BOBBIES",
            "BONDS",
            "BOXFRESH",
            "BREDA",
            "BRIXTOL",
            "BRIXTON",
            "BULLDOG",
            "CASIO",
            "CATARZI",
            "CHAMPION",
            "CHEAP MONDAY",
            "CLARKS ORIGINALS",
            "CLWR",
            "CONVERSE",
            "CORTICA",
            "CREATIVE RECREATION",
            "CRIMINAL DAMAGE",
            "DANIEL WELLINGTON",
            "LAUREN",
            "DESIGNSIX",
            "DICKIES",
            "DIESEL",
            "DR DENIM",
            "DUNE",
            "EA7",
            "EASTPAK",
            "EDWIN",
            "ELEMIS",
            "ELEMENT",
            "ELLESSE",
            "ELEVEN PARIS",
            "ESPRIT",
            "FIDELITY",
            "FILA VINTAGE",
            "FJALLRAVEN",
            "FOSSIL",
            "FRANK WRIGHT",
            "FRED PERRY",
            "HUGO BOSS",
            "GOORIN HATS",
            "GRENSON",
            "HAPPY SOCKS",
            "HAVAIANAS",
            "HELLY HANSEN",
            "HOUSE OF HOUNDS",
            "HUF",
            "HUNTER",
            "HYPE",
            "JACK WILLS",
            "JADED LONDON",
            "JEEPERS PEEPERS",
            "JEFFERY WEST",
            "JERUSALEM",
            "KAHUNA",
            "KAPPA",
            "KENZO",
            "KING APPAREL",
            "KOMONO",
            "LACOSTE",
            "LEE",
            "LIMIT",
            "LINDBERGH",
            "LOVE MOSCHINO",
            "MAJESTIC",
            "MINIMUM",
            "MONDAINE",
            "NEW ERA",
            "NIKE",
            "NIXON WATCHES",
            "NORTH FACE",
            "PALLADIUM",
            "PATAGONIA",
            "PENFIELD",
            "PERSOL",
            "PRINGLE",
            "PUMA",
            "QUIKSILVER",
            "RAINS"
        };

        private readonly IDictionary<string, string> _jacketTypes = new Dictionary<string, string>
        {
            { "Blazer","Summer" },
            { "Bomber jacket","Spring" },
            { "Donkey jacket","Autumn" },
            { "Duffle coat","Winter" },
            { "Duster coat","Autumn" },
            { "Flannel","Autumn" },
            { "Fleece jacket","Winter" },
            { "Jerkin","Autumn" },
            { "Motorcycle jacket","Autumn" },
            { "Sailor jacket","Summer" },
            { "Smoking jacket","Autumn" },
            { "Sport coat","Summer" },
            { "Tunic","Autumn" },
            { "Windbreaker","Spring" },
            { "Riding jacket","Summer" },
            { "Raincoat","Spring" },
            { "Puffer jacket","Spring" },
            { "Pea coat","Spring" },
            { "Norfolk jacket", "Spring" },
            { "Climb jacket","Winter" },
            { "Ski jacket","Winter" },
            { "Life jacket","Summer" }
        };

        private readonly IEnumerable<string> _jacketLook = new string[]
        {
            "Long",
            "Short",
            "Slim",
            "Wide"
        };

        private List<string> _colors = new List<string>
        {
            "White",
            "Green",
            "Blue",
            "Yellow",
            "Pink",
            "Red",
            "Orange",
            "Gray",
            "Purple"
        };

        private List<string> _sizes = new List<string>
        {
            "X-small",
            "Small",
            "Medium",
            "Large",
            "X-Large",
            "XX-Large",
            "XXX-Large"
        };
        
        
        private Injected<ReferenceConverter> ReferenceConverter { get; set; }
        private Injected<IContentRepository> ContentRepository { get; set; }
        private Injected<IPriceService> PriceService { get; set; }
        private Injected<ILinksRepository> LinksRepository { get; set; }
        private Injected<IWarehouseInventoryService> WarehouseInventoryService { get; set; }
        private Injected<ICurrentMarket> CurrentMarketService { get; set; }
        private Injected<IWarehouseRepository> WarehouseRepository { get; set; }

        private IMarket CurrentMarket { get { return CurrentMarketService.Service.GetCurrentMarket(); } }
        private IWarehouse DefaultWarehouse { get { return WarehouseRepository.Service.List().First(); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            //CreateContent();
            IndexJackets();
            //AddInventoryForAllJackets();
        }

        private void AddInventoryForAllJackets()
        {
            var menJacketNode = ReferenceConverter.Service.GetContentLink("jackets", CatalogContentType.CatalogNode);
            var winterJackets = ContentRepository.Service.GetChildren<JacketProduct>(menJacketNode, CultureInfo.InvariantCulture);
            foreach (var winterJacketProduct in winterJackets)
            {
                AddInventory(winterJacketProduct.Code);
            }
        }

        private void CreateContent()
        {
            var menNode = ReferenceConverter.Service.GetContentLink("mens", CatalogContentType.CatalogNode);
            var menJacketNode = ReferenceConverter.Service.GetContentLink("jackets", CatalogContentType.CatalogNode);
            CreateNodes(menNode, menJacketNode, "jackets");

            CreateJackets(menJacketNode);

            var womenNode = ReferenceConverter.Service.GetContentLink("womens");
            var womenJacketNode = ReferenceConverter.Service.GetContentLink("womenjackets");
            CreateNodes(womenNode, womenJacketNode, "womenjackets");

            CreateJackets(womenJacketNode);
        }

        private void IndexJackets()
        {
            var menJacketNode = ReferenceConverter.Service.GetContentLink("jackets", CatalogContentType.CatalogNode);
            var nodeJacket = ContentRepository.Service.Get<NodeContent>(menJacketNode);

            var winterJackets = ContentRepository.Service.GetChildren<JacketProduct>(menJacketNode, CultureInfo.InvariantCulture);
            foreach (var winterJacketProduct in winterJackets)
            {
                if (winterJacketProduct is WinterJacketProduct)
                {
                    continue;
                }

                var image = nodeJacket.CommerceMediaCollection.SingleOrDefault(x => x.GroupName.Equals(winterJacketProduct.Season, StringComparison.OrdinalIgnoreCase));
                if (image == null)
                {
                    image = nodeJacket.CommerceMediaCollection.SingleOrDefault(x => x.GroupName.Equals("other", StringComparison.OrdinalIgnoreCase));
                }
                if (image != null)
                {
                    var writableItem = winterJacketProduct.CreateWritableClone<JacketProduct>();
                    if (writableItem.CommerceMediaCollection.Any())
                    {
                        if (writableItem.CommerceMediaCollection[0].GroupName.Equals(winterJacketProduct.Season,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        writableItem.CommerceMediaCollection[0] = image;
                    }
                    else
                    {
                        writableItem.CommerceMediaCollection.Add(image);
                    }
                    
                    ContentRepository.Service.Save(writableItem, SaveAction.Publish, AccessLevel.NoAccess);
                }

                //SearchClient.Instance.Index(winterJacketProduct);
            }
        }

        private void CreateNodes(ContentReference parent, ContentReference contentLink, string name)
        {
            NodeContent nodeContent;
            if (ContentRepository.Service.TryGet(contentLink, out nodeContent))
            {
                return;
            }

            nodeContent = ContentRepository.Service.GetDefault<NodeContent>(parent);
            nodeContent.Name = name;
            nodeContent.Code = name;

            ContentRepository.Service.Save(nodeContent, SaveAction.Publish, AccessLevel.NoAccess);
        }

        private void CreateJackets(ContentReference node)
        {
            var nodeContent = ContentRepository.Service.Get<NodeContent>(node);
            foreach (var jacketBrand in _jacketBrands)
            {
                foreach (var jacketType in _jacketTypes)
                {
                    foreach (var jacketLook in _jacketLook)
                    { 
                        var code = String.Concat(jacketBrand, "_", jacketType.Key, "_", jacketType.Value, "_", jacketLook);
                        var contentLink = ReferenceConverter.Service.GetContentLink(code, CatalogContentType.CatalogEntry);

                        JacketProduct jacketProduct;
                        if (ContentRepository.Service.TryGet(contentLink, out jacketProduct))
                        {
                            continue;
                        }

                        if (jacketType.Value == "Winter")
                        {
                            var winterJacketProduct = ContentRepository.Service.GetDefault<WinterJacketProduct>(node);
                            winterJacketProduct.Degree = GetRandomNumber(-40, 0);

                            jacketProduct = winterJacketProduct;
                        }
                        else
                        {
                            jacketProduct = ContentRepository.Service.GetDefault<JacketProduct>(node);
                        }

                        jacketProduct.Code = code;
                        jacketProduct.Name = code;
                        jacketProduct.Brand = jacketBrand;
                        jacketProduct.Season = jacketType.Value;
                        jacketProduct.JacketType = jacketType.Key;
                        jacketProduct.Look = jacketLook;

                        var productLink = ContentRepository.Service.Save(jacketProduct, SaveAction.Publish, AccessLevel.NoAccess);
                        AddImage(jacketProduct, nodeContent);

                        CreateVariations(nodeContent, productLink, code);
                    }
                }
            }
        }

        private void CreateVariations(NodeContent node, ContentReference product, string productCode)
        {
            var colorsCount = _colors.Count();

            var startColor = GetRandomNumber(0, 3);
            var endColor = GetRandomNumber(3, colorsCount);
            if(endColor <= startColor)
            {
                endColor = startColor + 1;
            }

            for(var i = startColor; i < endColor - 1; i++)
            {
                var color = _colors[i];
                var sizesCount = _sizes.Count();

                var startSizes = GetRandomNumber(0, 3);
                var endSize = GetRandomNumber(3, sizesCount);
                if (endSize <= startSizes)
                {
                    endSize = startSizes + 1;
                }

                for (var j = startSizes; j < endSize - 1; j++)
                {
                    var size = _sizes[j];

                    var code = String.Concat(productCode, "_", color, "_", size);
                    var contentLink = ReferenceConverter.Service.GetContentLink(code);

                    FashionVariant variant;
                    if (ContentRepository.Service.TryGet(contentLink, out variant))
                    {
                        continue;
                    }

                    variant = ContentRepository.Service.GetDefault<FashionVariant>(node.ContentLink);
                    variant.Name = code;
                    variant.Code = code;
                    variant.Size = size;
                    variant.Color = color;

                    var variation = ContentRepository.Service.Save(variant, SaveAction.Publish, AccessLevel.NoAccess);
                    LinksRepository.Service.UpdateRelation(new ProductVariation
                    {
                        Source = product, 
                        Target = variation, 
                        SortOrder = 100, 
                        GroupName = EntryRelation.DefaultGroupName, 
                        Quantity = EntryRelation.DefaultQuantity
                    });

                    AddImage(variant, node);

                    var inventoryTask = new Task(() => AddInventory(code));
                    inventoryTask.Start();

                    var priceTask = new Task(() => AddPrice(code));
                    priceTask.Start();
                }
            }
        }

        private void AddImage(EntryContentBase content, NodeContent parent)
        {
            var mediaIndex = content is WinterJacketProduct ? 0 : 1;
            var media = parent.CommerceMediaCollection[mediaIndex];
            content.CommerceMediaCollection.Add(media);
        }

        private void AddPrice(string code)
        {
            var price = GetRandomNumber(1, 1000);
            var catalogKey = new CatalogKey(AppContext.Current.ApplicationId, code);

            PriceService.Service.SetCatalogEntryPrices(catalogKey, new[]
            { new PriceValue
                {
                    CatalogKey = catalogKey,
                    CustomerPricing = CustomerPricing.AllCustomers,
                    MarketId = CurrentMarket.MarketId,
                    MinQuantity = 1,
                    UnitPrice = new Money(price, CurrentMarket.DefaultCurrency),
                    ValidFrom = DateTime.Now,
                    ValidUntil = DateTime.Now.AddYears(10)
                }
            });    
        }

        private void AddInventory(string code)
        {
            WarehouseInventoryService.Service.Save(new WarehouseInventory
            {
                CatalogKey = new CatalogKey(AppContext.Current.ApplicationId, code),
                InStockQuantity = GetRandomNumber(1,100),
                BackorderAvailabilityDate = DateTime.Now,
                PreorderAvailabilityDate = DateTime.Now,
                WarehouseCode = DefaultWarehouse.Code
            });
        }

        private int GetRandomNumber(int lower, int higher)
        {
            var r = new Random();
            return r.Next(lower, higher > lower ? higher : lower);
        }
    }
}