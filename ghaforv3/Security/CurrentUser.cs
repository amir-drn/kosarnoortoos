using System.Linq;
using System.Web;
using Ghaforv3.Models;

namespace Ghaforv3.Security
{
    // کاربر واردشده را از دیتابیس می‌خواند (یک بار در هر درخواست)
    public static class CurrentUser
    {
        public static User Get(HttpContextBase ctx)
        {
            if (ctx.User == null || !ctx.User.Identity.IsAuthenticated) return null;
            var cached = ctx.Items["__me"] as User;
            if (cached != null) return cached;
            int id;
            if (!int.TryParse(ctx.User.Identity.Name, out id)) return null;
            using (var db = new Ghaforv3Db())
            {
                db.Configuration.ProxyCreationEnabled = false;
                var u = db.Users.FirstOrDefault(x => x.Id == id);
                ctx.Items["__me"] = u;
                return u;
            }
        }
    }
}
