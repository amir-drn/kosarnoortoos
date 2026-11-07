using System.IO;
using System.Web;

namespace Ghaforv3.Helpers
{
    public static class ImageHelper
    {
        private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // اگر ImageUrl در دیتابیس پر باشد همان را برمی‌گرداند.
        // وگرنه دنبال فایلی به نام Slug در Content/images می‌گردد؛ اگر نبود null.
        public static string Resolve(HttpServerUtilityBase server, string imageUrl, string slug)
        {
            if (!string.IsNullOrEmpty(imageUrl)) return imageUrl;
            foreach (var ext in Extensions)
            {
                var path = "~/Content/images/" + slug + ext;
                if (File.Exists(server.MapPath(path))) return path;
            }
            return null;
        }
    }
}