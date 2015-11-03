using System;
using System.Collections.Generic;
using EPiServer.Find;

namespace EPiServer.Reference.Commerce.Site.Features.Find
{
    public class ContentLinkOrders
    {
        [Id]
        public int Id { get; set; }
        public IEnumerable<string> ContentLinkStrings { get; set; }
        public DateTime OrderDate { get; set; }
    }
}