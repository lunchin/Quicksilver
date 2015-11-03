using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    [CatalogContentType(GUID = "110ebcfc-c989-4272-8f14-c6d079f56183",
        MetaClassName = "ShirtProduct",
        DisplayName = "Shirt product",
        Description = "Display shirt product")]
    public class ShirtProduct : FashionProduct, ISeason
    {
        public virtual string Season { get; set; }
    }
}