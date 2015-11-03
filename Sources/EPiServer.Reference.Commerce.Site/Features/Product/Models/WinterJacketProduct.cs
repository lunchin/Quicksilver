using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    [CatalogContentType(GUID = "550ebcfc-c989-4272-8f94-c6d079f56183",
        MetaClassName = "WinterJacketProduct",
        DisplayName = "Winter jacket product",
        Description = "Display winter jacket product")]
    public class WinterJacketProduct : JacketProduct, IDegree
    {
        public virtual int Degree { get; set; }
    }
}