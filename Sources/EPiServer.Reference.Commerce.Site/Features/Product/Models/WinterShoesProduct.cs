using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    [CatalogContentType(GUID = "230ebcfc-c989-4272-8f94-c6d079f56183",
        MetaClassName = "WinterShoesoduct",
        DisplayName = "Winter shoes product",
        Description = "Display winter shoes product")]
    public class WinterShoesProduct : ShoesProduct, IDegree
    {
        public virtual int Degree { get; set; }
    }
}