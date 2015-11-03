using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    [CatalogContentType(GUID = "550ebcfc-c989-4272-8f94-c6d079f56182",
        MetaClassName = "JacketProduct",
        DisplayName = "Jacket product",
        Description = "Display jacket product")]
    public class JacketProduct : FashionProduct, ISeason
    {
        public virtual string Season { get; set; }

        public virtual string JacketType { get; set; }

        public virtual string Look { get; set; }
    }
}