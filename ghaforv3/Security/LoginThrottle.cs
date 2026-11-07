using System;
using System.Web;
using System.Web.Caching;

namespace Ghaforv3.Security
{
    // محدودیت تلاش ورود (در حافظهٔ سرور):
    //  • ۵ تلاش ناموفق برای هر «شماره + آی‌پی» → ۱۰ دقیقه قفل
    //  • ۲۰ تلاش ناموفق از یک آی‌پی (روی شماره‌های مختلف) → ۱۰ دقیقه قفل
    // همچنین یک شمارندهٔ عمومی برای عملیات‌های دیگر (مثل ثبت‌نام) دارد.
    public static class LoginThrottle
    {
        private const int MaxFails = 5;
        private const int MaxFailsPerIp = 20;
        private static readonly TimeSpan LockTime = TimeSpan.FromMinutes(10);

        private static string Key(string mobile, string ip) { return "login-fail:" + mobile + ":" + ip; }
        private static string IpKey(string ip) { return "login-fail-ip:" + ip; }

        private static int Get(string key) { var v = HttpRuntime.Cache[key]; return v == null ? 0 : (int)v; }
        private static void Bump(string key, TimeSpan life)
        {
            // ساده و کافی برای یک سرور؛ اگر چند سرور دارید از ذخیرهٔ مشترک (مثل Redis/SQL) استفاده کنید
            HttpRuntime.Cache.Insert(key, Get(key) + 1, null, DateTime.UtcNow.Add(life), Cache.NoSlidingExpiration);
        }

        public static bool IsLocked(string mobile, string ip)
        {
            return Get(Key(mobile, ip)) >= MaxFails || Get(IpKey(ip)) >= MaxFailsPerIp;
        }

        public static void Fail(string mobile, string ip)
        {
            Bump(Key(mobile, ip), LockTime);
            Bump(IpKey(ip), LockTime);
        }

        public static void Clear(string mobile, string ip) { HttpRuntime.Cache.Remove(Key(mobile, ip)); }

        // شمارندهٔ عمومی: true یعنی از سقف گذشته (مثلاً ثبت‌نام بیش از ۱۰ بار در ساعت از یک آی‌پی)
        public static bool Hit(string bucket, string ip, int max, TimeSpan window)
        {
            var key = "hit:" + bucket + ":" + ip;
            if (Get(key) >= max) return true;
            Bump(key, window);
            return false;
        }
    }
}
