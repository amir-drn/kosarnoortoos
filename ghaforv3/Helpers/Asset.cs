using System;
using System.Collections.Concurrent;
using System.IO;
using System.Web;

namespace Ghaforv3.Helpers
{
    // آدرس فایل‌های CSS/JS را با شمارهٔ نسخه (زمان ویرایش فایل) می‌سازد؛
    // این‌طور می‌شود فایل‌ها را مدت زیادی در مرورگر کش کرد و با هر تغییر، نسخهٔ جدید خودکار دانلود می‌شود
    public static class Asset
    {
        private static readonly ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string, string>();

        public static string Url(string virtualPath)
        {
            var resolved = VirtualPathUtility.ToAbsolute(virtualPath);
            string v = Cache.GetOrAdd(virtualPath, p =>
            {
                try { return File.GetLastWriteTimeUtc(HttpContext.Current.Server.MapPath(p)).Ticks.ToString("x"); }
                catch (Exception) { return "0"; }
            });
            return resolved + "?v=" + v;
        }
    }
}
