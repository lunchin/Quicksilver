using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Validation;
using EPiServer.Core;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPiServer.Reference.Commerce.Site.Features.Discounts
{
    public class RewardMembersDiscountResult : IPromotionResult
    {
        private readonly IMarket _market;
        private readonly MonetaryReward _monetaryReward;
        private readonly Dictionary<ContentReference, ILineItem> _affectedLineItems;

        public RewardMembersDiscountResult(MonetaryReward reward, 
            FulfillmentStatus status, 
            string description) : this(null, reward, status, description, null)
        {
        }

        public RewardMembersDiscountResult(IMarket market, MonetaryReward reward,
           FulfillmentStatus status,
           string description,
           Dictionary<ContentReference, ILineItem> affectedLineItems)
        {
            ParameterValidator.ThrowIfNull(() => market, market);
            ParameterValidator.ThrowIfNull(() => reward, reward);
            _market = market;
            _monetaryReward = reward;
            Status = status;
            Description = description;
            _affectedLineItems = affectedLineItems;
        }

        public string Description { get; private set; }

        public FulfillmentStatus Status { get; private set; }

        public IEnumerable<PromotionInformation> ApplyReward()
        {
            if (_affectedLineItems == null || !_affectedLineItems.Any())
            {
                yield return new PromotionInformation
                {
                    Description = Description
                };
            }
            else
            {
                
                foreach (var contentLink in _affectedLineItems.Keys)
                {
                    var lineItem = _affectedLineItems[contentLink];
                    SetOrderLevelDiscountAmount(lineItem);

                    yield return new PromotionInformation
                    {
                        Description = Description,
                        SavedAmount = lineItem.OrderLevelDiscountAmount,
                        ContentLink = contentLink,
                        IsActive = true,
                    };
                }
            }
        }

        private void SetOrderLevelDiscountAmount(ILineItem lineItem)
        {
            var percentageOffTotal = 0m;
            if (_monetaryReward.UseAmounts)
            {
                var amount = _monetaryReward.Amounts.FirstOrDefault(reward => reward.Currency.Equals(_market.DefaultCurrency));
                if (amount == new Money() || amount.Amount < 0)
                {
                    lineItem.OrderLevelDiscountAmount = 0m;
                    return;
                }
                var totalCost = GetTotalCost(lineItem.LineItemId);
                percentageOffTotal = amount.Amount / totalCost;
            }
            else
            {
                percentageOffTotal = (_monetaryReward.Percentage / 100);
            }
            
            lineItem.OrderLevelDiscountAmount = Math.Floor((percentageOffTotal * ((lineItem.PlacedPrice * lineItem.Quantity) - (lineItem.LineItemDiscountAmount))) * 100) * 0.01m;
        }

        private decimal GetTotalCost(int lineItemId)
        {
            return _affectedLineItems.Values
                .Where(lineItems => lineItems.LineItemId != lineItemId)
                .Sum(lineItem => GetLineItemCost(lineItem));
        }

        private decimal GetLineItemCost(ILineItem lineItem)
        {
            var adjustedPrice = (lineItem.PlacedPrice * lineItem.Quantity) - (lineItem.OrderLevelDiscountAmount + lineItem.LineItemDiscountAmount);
            return adjustedPrice < 0
                ? 0
                : adjustedPrice;
        }
    }
}