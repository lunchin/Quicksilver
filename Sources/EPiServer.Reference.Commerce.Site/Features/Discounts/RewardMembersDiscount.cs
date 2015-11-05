using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Marketing;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.Discounts
{
    [ContentType(GUID = "0d9f4843-d610-4955-a79b-888c86cf0bd8")]
    public class RewardMembersDiscount : OrderPromotion
    {
        
        [AllowedTypes(typeof(EntryContentBase), typeof(NodeContent))]
        [Display(Order = 10)]
        public virtual IList<ContentReference> ConditionItems { get; set; }

        [Display(Order = 20)]
        public virtual bool MatchRecursive { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Order = 30)]
        public virtual int RequiredQuantity { get; set; }

        [Display(Order = 40)]
        public virtual MonetaryReward Reward { get; set; }
    }
}