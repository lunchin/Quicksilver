using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    [CatalogContentType(GUID = "120ebcfc-c989-4672-8f94-c6d079f56183",
        MetaClassName = "ShoesProduct",
        DisplayName = "Shoes product",
        Description = "Display shoes product")]
    public class ShoesProduct : FashionProduct, ISeason
    {
        public virtual string Season { get; set; }
    }
}