using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Ghaforv3.Models;
using Newtonsoft.Json.Linq;

namespace Ghaforv3.Helpers
{
    public class PayStart { public bool Ok; public string Authority; public string RedirectUrl; public string Error; }
    public class PayVerify { public bool Ok; public string RefId; public string Error; }

    public interface IPaymentGateway
    {
        string Name { get; }
        PayStart Start(Order order, string callbackUrl, string mobile, string demoUrl);
        PayVerify Verify(Order order, string authority);
    }

    // درگاه زرین‌پال (API نسخهٔ ۴). مبلغ سفارش تومان است و به ریال (×۱۰) فرستاده می‌شود.
    public class ZarinPalGateway : IPaymentGateway
    {
        public string Name { get { return "ZarinPal"; } }
        private const string Base = "https://payment.zarinpal.com/pg/v4/payment/";

        private static JObject Post(string endpoint, JObject body)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var req = (HttpWebRequest)WebRequest.Create(Base + endpoint);
            req.Method = "POST"; req.ContentType = "application/json"; req.Accept = "application/json"; req.Timeout = 20000;
            var bytes = Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None));
            using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
            try
            {
                using (var res = (HttpWebResponse)req.GetResponse())
                using (var r = new StreamReader(res.GetResponseStream(), Encoding.UTF8)) return JObject.Parse(r.ReadToEnd());
            }
            catch (WebException ex)
            {
                if (ex.Response == null) throw;
                using (var r = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8)) return JObject.Parse(r.ReadToEnd());
            }
        }

        public PayStart Start(Order order, string callbackUrl, string mobile, string demoUrl)
        {
            var merchant = Site.ZarinPalMerchant;
            if (merchant.Length == 0) return new PayStart { Error = "شناسهٔ پذیرندهٔ زرین‌پال (ZarinPalMerchantId) تنظیم نشده است" };
            try
            {
                var j = Post("request.json", new JObject
                {
                    { "merchant_id", merchant },
                    { "amount", (long)(order.TotalAmount * 10) },
                    { "callback_url", callbackUrl },
                    { "description", "سفارش شمارهٔ " + order.Id },
                    { "metadata", new JObject { { "mobile", mobile ?? "" } } }
                });
                var data = j["data"] as JObject;
                if (data != null && (int?)data["code"] == 100 && !string.IsNullOrEmpty((string)data["authority"]))
                {
                    var a = (string)data["authority"];
                    return new PayStart { Ok = true, Authority = a, RedirectUrl = "https://payment.zarinpal.com/pg/StartPay/" + a };
                }
                return new PayStart { Error = "درگاه پرداخت درخواست را نپذیرفت. دوباره تلاش کنید." };
            }
            catch (Exception) { return new PayStart { Error = "ارتباط با درگاه پرداخت برقرار نشد. دوباره تلاش کنید." }; }
        }

        public PayVerify Verify(Order order, string authority)
        {
            try
            {
                var j = Post("verify.json", new JObject
                {
                    { "merchant_id", Site.ZarinPalMerchant },
                    { "amount", (long)(order.TotalAmount * 10) },
                    { "authority", authority }
                });
                var data = j["data"] as JObject;
                int? code = data == null ? null : (int?)data["code"];
                if (code == 100 || code == 101) return new PayVerify { Ok = true, RefId = Convert.ToString((object)data["ref_id"]) };
                return new PayVerify { Error = "پرداخت تأیید نشد" };
            }
            catch (Exception) { return new PayVerify { Error = "تأیید پرداخت با خطا روبه‌رو شد" }; }
        }
    }

    // درگاه آزمایشی برای توسعه: یک صفحهٔ «بانک» ساختگی. فقط روی خود سرور (localhost) کار می‌کند
    // تا اگر اشتباهاً روی سرور واقعی ماند، کسی نتواند بدون پرداخت سفارش «پرداخت‌شده» بسازد.
    public class DemoGateway : IPaymentGateway
    {
        public string Name { get { return "Demo"; } }

        public PayStart Start(Order order, string callbackUrl, string mobile, string demoUrl)
        {
            var a = "DEMO" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            return new PayStart { Ok = true, Authority = a, RedirectUrl = demoUrl + "?authority=" + a };
        }

        public PayVerify Verify(Order order, string authority)
        {
            return authority != null && authority.StartsWith("DEMO", StringComparison.Ordinal)
                ? new PayVerify { Ok = true, RefId = "DEMO-" + order.Id }
                : new PayVerify { Error = "پرداخت تأیید نشد" };
        }
    }

    public static class Gateways
    {
        // null یعنی درگاه در دسترس نیست (مثلاً Demo روی سرور واقعی)
        public static IPaymentGateway Current(HttpRequestBase req)
        {
            if (string.Equals(Site.Gateway, "ZarinPal", StringComparison.OrdinalIgnoreCase)) return new ZarinPalGateway();
            return req.IsLocal ? new DemoGateway() : null;
        }

        public static IPaymentGateway ByName(string name, HttpRequestBase req)
        {
            if (name == "ZarinPal") return new ZarinPalGateway();
            if (name == "Demo" && req.IsLocal) return new DemoGateway();
            return null;
        }
    }
}
