using System;
using System.Configuration;
using System.Globalization;
using System.Web;

namespace Ghaforv3.Helpers
{
    // تنظیمات فروشگاه از appSettings در Web.config
    public static class Site
    {
        private static string S(string key, string fallback)
        {
            var v = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
        }
        private static decimal D(string key, decimal fallback)
        {
            decimal v;
            return decimal.TryParse(S(key, ""), NumberStyles.Number, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        public static decimal ShippingFee { get { return D("ShippingFee", 0); } }              // تومان؛ ۰ = رایگان
        public static decimal FreeShippingFrom { get { return D("FreeShippingFrom", 0); } }    // ارسال رایگان از این مبلغ به بالا؛ ۰ = غیرفعال
        public static int ReserveMinutes { get { return (int)Math.Max(5, D("ReserveMinutes", 30)); } }  // مدت نگه‌داشتن موجودی سفارش پرداخت‌نشده

        public static string Phone { get { return S("ContactPhone", ""); } }
        public static string Email { get { return S("ContactEmail", ""); } }
        public static string Address { get { return S("ContactAddress", ""); } }
        public static string Hours { get { return S("ContactHours", "هر روز ۹ صبح تا ۱۰ شب"); } }
        public static string Instagram { get { return S("SocialInstagram", ""); } }
        public static string Telegram { get { return S("SocialTelegram", ""); } }
        public static string WhatsApp { get { return S("SocialWhatsApp", ""); } }

        public static string Gateway { get { return S("PaymentGateway", "Demo"); } }          // Demo | ZarinPal
        public static string ZarinPalMerchant { get { return S("ZarinPalMerchantId", ""); } }

        public static decimal Shipping(decimal subtotal)
        {
            var fee = ShippingFee;
            if (fee <= 0) return 0;
            var free = FreeShippingFrom;
            return (free > 0 && subtotal >= free) ? 0 : fee;
        }

        // فقط «دامنه» (بدون مسیر)؛ برای آدرس بازگشت درگاه پرداخت
        public static string Origin(HttpRequestBase req)
        {
            var configured = S("SiteUrl", "");
            if (configured.Length > 0)
            {
                try { return new Uri(configured).GetLeftPart(UriPartial.Authority); } catch (UriFormatException) { }
            }
            return req.Url.GetLeftPart(UriPartial.Authority);
        }

        // آدرس پایهٔ سایت برای sitemap و canonical (اگر SiteUrl تنظیم نشده از خود درخواست گرفته می‌شود)
        public static string BaseUrl(HttpRequestBase req)
        {
            var configured = S("SiteUrl", "");
            if (configured.Length > 0) return configured.TrimEnd('/');
            return req.Url.GetLeftPart(UriPartial.Authority) + (req.ApplicationPath == "/" ? "" : req.ApplicationPath);
        }
    }
}
