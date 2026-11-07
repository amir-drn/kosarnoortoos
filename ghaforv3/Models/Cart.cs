using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Ghaforv3.Models
{
    [Serializable]
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get { return UnitPrice * Quantity; } }
    }

    [Serializable]
    public class Cart
    {
        public List<CartItem> Items { get; set; }
        public Cart() { Items = new List<CartItem>(); }
        public int Count { get { return Items.Sum(i => i.Quantity); } }
        public decimal Total { get { return Items.Sum(i => i.Total); } }
    }

    // سبد را در Session نگه می‌دارد
    public static class CartStore
    {
        private const string Key = "cart";
        public const int MaxPerLine = 99;

        // قیمت و موجودی سبد را با دیتابیس هماهنگ می‌کند؛ کالای غیرفعال یا ناموجود از سبد حذف می‌شود
        public static void Sync(Ghaforv3Db db, Cart cart)
        {
            if (cart.Items.Count == 0) return;
            var ids = cart.Items.Select(i => i.ProductId).ToList();
            var live = db.Products.Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.Title, p.Slug, p.ImageUrl, p.Price, p.Stock, p.IsActive }).ToList();
            foreach (var item in cart.Items.ToList())
            {
                var p = live.FirstOrDefault(x => x.Id == item.ProductId);
                if (p == null || !p.IsActive || p.Stock <= 0) { cart.Items.Remove(item); continue; }
                item.Title = p.Title; item.Slug = p.Slug; item.ImageUrl = p.ImageUrl; item.UnitPrice = p.Price;
                item.Quantity = Math.Max(1, Math.Min(item.Quantity, Math.Min(p.Stock, MaxPerLine)));
            }
        }

        public static Cart Get(HttpSessionStateBase session)
        {
            var cart = session[Key] as Cart;
            if (cart == null) { cart = new Cart(); session[Key] = cart; }
            return cart;
        }
    }
}
