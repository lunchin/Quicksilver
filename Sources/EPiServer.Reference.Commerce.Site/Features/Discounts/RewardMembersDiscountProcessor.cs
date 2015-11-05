using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Validation;
using EPiServer.Core;
using EPiServer.Security;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EPiServer.Reference.Commerce.Site.Features.Discounts
{
    public class RewardMembersDiscountProcessor : PromotionProcessorBase<RewardMembersDiscount>
    {
        private readonly IContentLoader _contentLoader;
        private readonly ILinksRepository _linksRepository;
        private readonly ReferenceConverter _referenceConverter;


        public RewardMembersDiscountProcessor(IContentLoader contentLoader, ILinksRepository linksRepository, ReferenceConverter referenceConverter)
        {
            ParameterValidator.ThrowIfNull(() => contentLoader, contentLoader);
            ParameterValidator.ThrowIfNull(() => linksRepository, linksRepository);
            ParameterValidator.ThrowIfNull(() => referenceConverter, referenceConverter);
            _contentLoader = contentLoader;
            _linksRepository = linksRepository;
            _referenceConverter = referenceConverter;
        }

        public override IPromotionResult Evaluate(IOrderGroup orderGroup, RewardMembersDiscount promotion)
        {
            IEnumerable<ILineItem> lineItemsInOrder = orderGroup.Forms.SelectMany(form => form.Shipments)
                                                   .SelectMany(shipment => shipment.LineItems).ToList();

            var contact = PrincipalInfo.CurrentPrincipal.GetCustomerContact();
            if (contact == null || !IsMember(contact))
            {
                return new RewardMembersDiscountResult(orderGroup.Market,
                    promotion.Reward,
                    FulfillmentStatus.NotFulfilled,
                    "The promotion is not fulfilled.",
                    null);
            }

            if (!MacthesCondition(lineItemsInOrder, promotion))
            {
                return new RewardMembersDiscountResult(orderGroup.Market, 
                    promotion.Reward, 
                    FulfillmentStatus.NotFulfilled,
                    "The promotion is not fulfilled.",
                    null);
            }

            var macthedItems = GetAllMatchedItems(lineItemsInOrder, promotion);

            if (!macthedItems.Any())
            {
                return new RewardMembersDiscountResult(orderGroup.Market,
                    promotion.Reward, 
                    FulfillmentStatus.NotFulfilled,
                    "The promotion is not fulfilled.",
                    null);
            }

            var totalmacthedItemsQuantity = macthedItems.Sum(x => x.Quantity);
            if (totalmacthedItemsQuantity < promotion.RequiredQuantity)
            {
                return new RewardMembersDiscountResult(orderGroup.Market,
                    promotion.Reward, 
                    FulfillmentStatus.PartiallyFulfilled,
                    "The promotion is partially fulfilled.",
                    null);
            }

            var discountItems = GetItemsToApplyDiscountTo(lineItemsInOrder);

            return new RewardMembersDiscountResult(orderGroup.Market,
                promotion.Reward, 
                FulfillmentStatus.Fulfilled,
                "The promotion is fulfilled.", 
                discountItems);
        }
        
        private bool MacthesCondition(IEnumerable<ILineItem> allOrderItems, RewardMembersDiscount promotion)
        {
            if (!allOrderItems.Any())
            {
                return false;
            }

            if (!promotion.ConditionItems.Any())
            {
                return false;
            }

            var macthedItems = GetAllMatchedItems(allOrderItems, promotion);
            if (!macthedItems.Any())
            {
                return false;
            }
            return true;
        }

        protected virtual IList<ILineItem> GetAllMatchedItems(IEnumerable<ILineItem> lineItemsInOrder, RewardMembersDiscount promotion)
        {
            var targets = promotion.ConditionItems.Select(contentRef => _contentLoader.Get<CatalogContentBase>(contentRef));
            return targets.SelectMany(target => GetAllMatchedItemsByTarget(lineItemsInOrder, target, promotion.MatchRecursive))
                          .GroupBy(x => x.LineItemId)
                          .Select(x => x.First())
                          .ToList();
        }

        protected virtual IEnumerable<ILineItem> GetAllMatchedItemsByTarget(IEnumerable<ILineItem> lineItemsInOrder, IContent targetContent, bool matchRecursive)
        {
            var entryContent = targetContent as EntryContentBase;
            if (entryContent != null && targetContent is IPricing)
            {
                return GetAllMatchedItemsForPricingContentTarget(lineItemsInOrder, entryContent);
            }

            var product = targetContent as ProductContent;
            if (product != null)
            {
                return GetAllMatchedItemsForProductTarget(lineItemsInOrder, product);
            }

            var bundle = targetContent as BundleContent;
            if (bundle != null)
            {
                return GetAllMatchedItemsForBundleTarget(lineItemsInOrder, bundle);
            }

            var entries = GetContentUnderTarget(targetContent.ContentLink, matchRecursive);
            return entries.SelectMany(entry => GetAllMatchedItemsByTarget(lineItemsInOrder, entry, false));
        }

        private IEnumerable<EntryContentBase> GetContentUnderTarget(ContentReference target, bool matchRecursive)
        {
            if (matchRecursive)
            {
                var descendents = _contentLoader.GetDescendents(target);
                return _contentLoader.GetItems(descendents, CultureInfo.InvariantCulture).OfType<EntryContentBase>()
                    .Where(entry => entry is IPricing || entry is BundleContent);
            }

            return _contentLoader.GetChildren<EntryContentBase>(target).Where(entry => entry is IPricing || entry is BundleContent);
        }

        private IEnumerable<ILineItem> GetAllMatchedItemsForPricingContentTarget(IEnumerable<ILineItem> lineItemsInOrder, EntryContentBase entry)
        {
            return lineItemsInOrder.Where(lineItem => string.Equals(lineItem.Code, entry.Code, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<ILineItem> GetAllMatchedItemsForProductTarget(IEnumerable<ILineItem> lineItemsInOrder, ProductContent product)
        {
            var matchedItems = new List<ILineItem>();
            var productEntriesLinks = product.GetVariants(_linksRepository);
            foreach (var entryLink in productEntriesLinks)
            {
                var entry = _contentLoader.Get<EntryContentBase>(entryLink);
                var matchedVariant = GetAllMatchedItemsByTarget(lineItemsInOrder, entry, true);
                if (!matchedVariant.Any())
                {
                    continue;
                }
                matchedItems.AddRange(matchedVariant);
            }

            return matchedItems;
        }

        private IEnumerable<ILineItem> GetAllMatchedItemsForBundleTarget(IEnumerable<ILineItem> lineItemsInOrder, BundleContent bundle)
        {
            var matchedItems = new List<ILineItem>();
            var bundleEntriesLinks = bundle.GetEntries(_linksRepository);
            foreach (var entryLink in bundleEntriesLinks)
            {
                var entry = _contentLoader.Get<EntryContentBase>(entryLink);
                var matchedItemOfBundle = GetAllMatchedItemsByTarget(lineItemsInOrder, entry, true);
                //In case the target is bundle, the promotion is applicable to the order when and only when the order has all items of the bundle.
                if (!matchedItemOfBundle.Any())
                {
                    matchedItems = new List<ILineItem>();
                    break;
                }
                matchedItems.AddRange(matchedItemOfBundle);
            }

            return matchedItems;
        }

        private Dictionary<ContentReference, ILineItem> GetItemsToApplyDiscountTo(IEnumerable<ILineItem> lineItems)
        {
            return lineItems.ToDictionary(lineItem => _referenceConverter.GetContentLink(lineItem.Code), lineItem => lineItem);
        }

        private bool IsMember(CustomerContact contact)
        {
            return contact["IsMember"] != null && Convert.ToBoolean(contact["IsMember"]);
        }
    }
}