using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Ghaforv3.Models;

namespace Ghaforv3.Helpers
{
    // منطق ثبت سفارش و موجودی. موجودی با یک دستور SQL «شرطی» کم می‌شود
    // (UPDATE ... WHERE Stock >= تعداد) تا دو خریدار هم‌زمان نتوانند یک کالا را بیش از موجودی بخرند.
    public static class OrderService
    {
        public const byte Pending = 0, Paid = 1, Shipped = 2, Delivered = 3, Canceled = 4;

        // موجودی را رزرو می‌کند؛ اگر کافی نبود false
        private static bool Reserve(Ghaforv3Db db, int productId, int qty)
        {
            return db.Database.ExecuteSqlCommand(
                "UPDATE dbo.Products SET Stock = Stock - @p0 WHERE Id = @p1 AND Stock >= @p0", qty, productId) > 0;
        }

        private static void Release(Ghaforv3Db db, int productId, int qty)
        {
            db.Database.ExecuteSqlCommand("UPDATE dbo.Products SET Stock = Stock + @p0 WHERE Id = @p1", qty, productId);
        }

        // سفارش را از روی سبد می‌سازد. قیمت‌ها همیشه از دیتابیس خوانده می‌شود، نه از سبد/مرورگر.
        // موفق: null و order پر می‌شود؛ ناموفق: متن خطا
        public static string Create(Ghaforv3Db db, int userId, int addressId, IEnumerable<CartItem> cart, out Order order)
        {
            order = null;
            var lines = cart.ToList();
            if (lines.Count == 0) return "سبد خرید خالی است";

            using (var tx = db.Database.BeginTransaction())
            {
                decimal subtotal = 0;
                var items = new List<OrderItem>();
                foreach (var line in lines)
                {
                    var p = db.Products.FirstOrDefault(x => x.Id == line.ProductId && x.IsActive);
                    if (p == null) return "یکی از کالاهای سبد دیگر موجود نیست";
                    int qty = Math.Max(1, Math.Min(line.Quantity, CartStore.MaxPerLine));
                    if (!Reserve(db, p.Id, qty)) return "موجودی «" + p.Title + "» کافی نیست";

                    // رنگ‌های انتخابی دوباره با گزینه‌های فعلی محصول سنجیده می‌شوند
                    var bodies = p.BodyColorList; var lights = p.LightColorList;
                    string body = bodies.Count > 0 ? line.BodyColor : null, light = lights.Count > 0 ? line.LightColor : null;
                    if ((bodies.Count > 0 && (body == null || !bodies.Contains(body))) || (lights.Count > 0 && (light == null || !lights.Contains(light))))
                        return "گزینه‌های رنگ «" + p.Title + "» دیگر معتبر نیست؛ آن را دوباره به سبد اضافه کن";

                    var title = p.Title.Length > 200 ? p.Title.Substring(0, 200) : p.Title;
                    items.Add(new OrderItem { ProductId = p.Id, Title = title, UnitPrice = p.Price, Quantity = qty, BodyColor = body, LightColor = light });
                    subtotal += p.Price * qty;
                }

                var o = new Order
                {
                    UserId = userId, AddressId = addressId, Status = Pending,
                    TotalAmount = subtotal + Site.Shipping(subtotal), CreatedAt = DateTime.Now
                };
                foreach (var it in items) o.OrderItems.Add(it);
                db.Orders.Add(o);
                db.SaveChanges();
                tx.Commit();
                order = o;
                return null;
            }
        }

        // سفارش‌های پرداخت‌نشده‌ای که مهلتشان گذشته لغو می‌شوند و موجودی برمی‌گردد
        public static void ReleaseExpired(Ghaforv3Db db)
        {
            var cutoff = DateTime.Now.AddMinutes(-Site.ReserveMinutes);
            var old = db.Orders.Include(o => o.OrderItems)
                .Where(o => o.Status == Pending && o.CreatedAt < cutoff).Take(50).ToList();
            foreach (var o in old) Cancel(db, o);
        }

        // لغو سفارش + برگرداندن موجودی (فقط اگر قبلاً لغو نشده)
        public static bool Cancel(Ghaforv3Db db, Order o)
        {
            if (o.Status == Canceled) return false;
            using (var tx = db.Database.BeginTransaction())
            {
                if (o.OrderItems == null || o.OrderItems.Count == 0)
                    db.Entry(o).Collection(x => x.OrderItems).Load();
                foreach (var it in o.OrderItems) Release(db, it.ProductId, it.Quantity);
                o.Status = Canceled;
                db.SaveChanges();
                tx.Commit();
            }
            return true;
        }

        // سفارش لغوشده‌ای که پولش در بانک کسر شده دوباره باز می‌شود؛ اگر موجودی نبود false
        public static bool Reopen(Ghaforv3Db db, Order o)
        {
            if (o.Status != Canceled) return true;
            using (var tx = db.Database.BeginTransaction())
            {
                db.Entry(o).Collection(x => x.OrderItems).Load();
                foreach (var it in o.OrderItems)
                    if (!Reserve(db, it.ProductId, it.Quantity)) return false;   // tx بدون commit برمی‌گردد = rollback
                o.Status = Paid;
                db.SaveChanges();
                tx.Commit();
            }
            return true;
        }

        // تغییر وضعیت توسط مدیر؛ پیام خطا یا null
        public static string SetStatus(Ghaforv3Db db, Order o, byte status)
        {
            if (status == o.Status) return null;
            if (o.Status == Canceled) return "سفارش لغوشده را نمی‌شود دوباره فعال کرد؛ یک سفارش جدید ثبت کنید";
            if (status == Canceled) { Cancel(db, o); return null; }
            if (o.Status == Pending && status > Paid) return "ابتدا باید پرداخت انجام شود";
            o.Status = status;
            db.SaveChanges();
            return null;
        }
    }
}
