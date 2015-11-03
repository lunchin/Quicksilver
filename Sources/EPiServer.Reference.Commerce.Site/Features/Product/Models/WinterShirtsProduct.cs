using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    [CatalogContentType(GUID = "210ebcfc-c989-4272-8f94-c6d019f56183",
        MetaClassName = "WinterShirtProduct",
        DisplayName = "Winter shirt product",
        Description = "Display winter shirt product")]
    public class WinterShirtsProduct : ShirtProduct, IDegree
    {
        public virtual int Degree { get; set; }
    }
}