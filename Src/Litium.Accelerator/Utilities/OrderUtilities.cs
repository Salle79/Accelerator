using Litium.Foundation;
using Litium.Foundation.Log;
using Litium.Foundation.Modules.ECommerce.Orders;
using Litium.Foundation.Modules.ECommerce.ShoppingCarts;
using Litium.Foundation.Security;
using Litium.Products;
using Litium.Products.StockStatusCalculator;
using Litium.Runtime.DependencyInjection;
using System;
using System.Linq;

namespace Litium.Accelerator.Utilities
{
    /// <summary>
    ///     Helper methods for commong operations of a ECommerce order, payments and its deliveries.
    /// </summary>
    [Service(ServiceType = typeof(OrderUtilities), Lifetime = DependencyLifetime.Singleton)]
    public class OrderUtilities
    {
        private readonly IStockStatusCalculator _stockStatusCalculator;
        private readonly InventoryItemService _inventoryItemService;
        private readonly CartAccessor _cartAccessor;
        private readonly VariantService _variantService;

        public OrderUtilities(
            CartAccessor cartAccessor,
            VariantService variantService,
            InventoryItemService inventoryItemService,
            IStockStatusCalculator stockStatusCalculator)
        {
            _stockStatusCalculator = stockStatusCalculator;
            _inventoryItemService = inventoryItemService;
            _cartAccessor = cartAccessor;
            _variantService = variantService;
        }

        /// <summary>
        ///     Returns true if the current state shopping cart has order carrier otherwise false.
        /// </summary>
        private bool HasOrder
        {
            get { return _cartAccessor.Cart.OrderCarrier != null; }
        }

        /// <summary>
        ///     Completes the payments for all reserved transactions in the order.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="token">The token.</param>
        public void CompletePayments(Order order, SecurityToken token)
        {
            if (order == null)
            {
                return;
            }

            try
            {
                foreach (var paymentInfo in order.PaymentInfo)
                {
                    if (paymentInfo.PaymentProvider.CanCompleteCurrentTransaction)
                    {
                        try
                        {
                            paymentInfo.PaymentProvider.CompletePayment(null, token);
                        }
                        catch (Exception ex)
                        {
                            Solution.Instance.Log.CreateLogEntry("Payment completion failed for order " +
                                                                 order.ExternalOrderID, ex, LogLevels.ERROR);
                        }
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Solution.Instance.Log.CreateLogEntry("Payment completion failed for order " + order.ExternalOrderID +
                                                     ". Check whether payment providers for the payments in this order is correctly initialized", ex, LogLevels.FATAL);
            }
            catch (Exception ex)
            {
                Solution.Instance.Log.CreateLogEntry("Payment completion failed for order " + order.ExternalOrderID, ex,
                    LogLevels.FATAL);
            }
        }

        /// <summary>
        ///     Reduces the stock balance for article in order.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="token">The token.</param>
        public void ReduceStockBalance(Order order, SecurityToken token)
        {
            if (order == null)
            {
                return;
            }

            var inventories = _stockStatusCalculator.GetInventories(new StockStatusCalculatorArgs
            {
                CountrySystemId = order.CountryID,
                UserSystemId = order.CustomerInfo.PersonID,
                WebSiteSystemId = order.WebSiteID,
            });

            try
            {
                var orderCarrier = order.GetAsCarrier(true, true, true, true, true, true);
                var articlesPurchased = from o in orderCarrier.OrderRows
                                        group o by o.ArticleNumber
                                        into g
                                        select new { ArticleNumber = g.Key, Quantity = g.Sum(p => p.Quantity) };

                foreach (var item in articlesPurchased)
                {
                    var article = _variantService.Get(item.ArticleNumber);
                    if (article != null)
                    {
                        var stockItems = inventories.Select(x => _inventoryItemService.Get(article.SystemId, x.SystemId));
                        var stock = (stockItems.FirstOrDefault(x => x?.InStockQuantity > 0) ?? stockItems.FirstOrDefault())?.MakeWritableClone();
                        //this will set the stock quantities to negative values, if purchased more than the available stocks.
                        //we expect this to be correct to show how much deficit is there for the given article.
                        if (stock != null)
                        {
                            stock.InStockQuantity -= item.Quantity;
                            using (Solution.Instance.SystemToken.Use("OrderUtilities.ReduceStockBalance"))
                            {
                                _inventoryItemService.Update(stock);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Solution.Instance.Log.CreateLogEntry("Could not reduce the stock quantity for order " +
                                                     order.ExternalOrderID, ex, LogLevels.ERROR);
            }
        }

        /// <summary>
        ///     Reorder order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="languageId">Language that is used when the product is published on the current web site.</param>
        public void ReOrder(Order order, Guid languageId)
        {
            foreach (var orderRow in order.OrderRows)
            {
                AddArticleToCart(orderRow.ArticleNumber, languageId, orderRow.Quantity);
            }
        }

        /// <summary>
        ///     Return or cancell all payments connected to an order.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="token">The token.</param>
        public void ReturnOrCancelAllPayments(Order order, SecurityToken token)
        {
            if (order == null)
            {
                return;
            }

            try
            {
                foreach (var paymentInfo in order.PaymentInfo)
                {
                    if (paymentInfo.PaymentProvider.CanReturnPayment)
                    {
                        try
                        {
                            paymentInfo.PaymentProvider.ReturnPayment(null, token);
                        }
                        catch (Exception ex)
                        {
                            Solution.Instance.Log.CreateLogEntry("Payment return failed for order " +
                                                                 order.ExternalOrderID, ex, LogLevels.ERROR);
                        }
                    }
                    else if (paymentInfo.PaymentProvider.CanCancelCurrentTransaction)
                    {
                        try
                        {
                            paymentInfo.PaymentProvider.CancelPayment(null, token);
                        }
                        catch (Exception ex)
                        {
                            Solution.Instance.Log.CreateLogEntry("Payment cancellation failed for order " +
                                                                 order.ExternalOrderID, ex, LogLevels.ERROR);
                        }
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Solution.Instance.Log.CreateLogEntry(
                    "Return or cancelling payments failed for order " + order.ExternalOrderID +
                    ". Check whether payment providers for the payments in this order is correctly initialized", ex, LogLevels.FATAL);
            }
            catch (Exception ex)
            {
                Solution.Instance.Log.CreateLogEntry("Return or cancelling payments failed for order " +
                                                     order.ExternalOrderID, ex, LogLevels.FATAL);
            }
        }

        /// <summary>
        ///     Add an article to the current shopping cart.
        /// </summary>
        /// <param name="articleNumber">Number of the article.</param>
        /// <param name="languageId">Language that is used when the product is published on the current web site.</param>
        /// <param name="quantity">Quantity of the article.</param>
        private void AddArticleToCart(string articleNumber, Guid languageId, decimal quantity)
        {
            // Article can not be added to the shoping cart if the cart has no order.
            if (!HasOrder)
            {
                return;
            }

            try
            {
                var article = _variantService.Get(articleNumber);

                // Add the article to the shoping cart if the article exists
                if (article != null)
                {
                    if (quantity > 0)
                    {
                        _cartAccessor.Cart.Add(
                            article.Id,
                            quantity,
                            string.Empty,
                            languageId);
                    }
                }
            }
            catch (Exception ex)
            {
                Solution.Instance.Log.CreateLogEntry("AddArticleToCart", ex, LogLevels.ERROR);
            }
        }
    }
}