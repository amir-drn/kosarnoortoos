using System;
using System.IO;
using System.Web;

namespace Ghaforv3.Helpers
{
    public static class ImageUpload
    {
        private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp" };
        private const int MaxBytes = 2 * 1024 * 1024;   // ۲ مگابایت
        private const string UrlPrefix = "~/Content/images/";

        // عکس را در Content/images ذخیره می‌کند و آدرس "~/Content/images/..." را برمی‌گرداند
        public static string Save(HttpServerUtilityBase server, HttpPostedFileBase file, string slug, out string error)
        {
            error = null;
            var ext = (Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();
            if (Array.IndexOf(Allowed, ext) < 0) { error = "فقط فرمت jpg، png یا webp مجاز است"; return null; }
            if (file.ContentLength <= 0 || file.ContentLength > MaxBytes) { error = "حجم عکس نباید بیشتر از ۲ مگابایت باشد"; return null; }

            // نوع فایل را از «محتوای واقعی» تشخیص می‌دهیم، نه از نام یا ContentType که کاربر می‌تواند جعل کند
            var kind = Sniff(file.InputStream);
            if (kind == null) { error = "فایل انتخاب‌شده عکس معتبر نیست"; return null; }
            ext = kind;

            // slug فقط حروف کوچک/عدد/خط‌تیره می‌پذیرد (جلوی ../ و مسیرهای مخرب)
            var safeSlug = System.Text.RegularExpressions.Regex.Replace((slug ?? "img").ToLowerInvariant(), "[^a-z0-9-]", "");
            if (safeSlug.Length == 0) safeSlug = "img";
            if (safeSlug.Length > 60) safeSlug = safeSlug.Substring(0, 60);

            var dir = server.MapPath("~/Content/images");
            Directory.CreateDirectory(dir);
            var name = safeSlug + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ext;
            file.InputStream.Position = 0;
            file.SaveAs(Path.Combine(dir, name));
            return UrlPrefix + name;
        }

        // فقط فایل‌هایی حذف می‌شوند که واقعاً «داخل» پوشهٔ Content/images باشند
        public static void Delete(HttpServerUtilityBase server, string url)
        {
            if (!IsSafeUrl(url)) return;
            try
            {
                var root = Path.GetFullPath(server.MapPath("~/Content/images")).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var full = Path.GetFullPath(server.MapPath(url));
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;
                if (File.Exists(full)) File.Delete(full);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
        }

        // آدرس عکس معتبر: ~/Content/images/<نام ساده>   (بدون .. و بدون زیرپوشه)
        public static bool IsSafeUrl(string url)
        {
            return !string.IsNullOrEmpty(url)
                && url.Length <= 300
                && System.Text.RegularExpressions.Regex.IsMatch(url, @"^~/Content/images/[A-Za-z0-9._() %-]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                && !url.Contains("..");
        }

        // jpg / png / webp را از چند بایت اول تشخیص می‌دهد؛ در غیر این صورت null
        private static string Sniff(Stream s)
        {
            var h = new byte[12];
            s.Position = 0;
            int n = s.Read(h, 0, h.Length);
            s.Position = 0;
            if (n < 12) return null;
            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return ".png";
            if (h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return ".webp";
            return null;
        }
    }
}
