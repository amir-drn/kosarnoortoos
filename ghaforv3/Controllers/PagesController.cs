using System.Web.Mvc;

namespace Ghaforv3.Controllers
{
    // صفحه‌های ثابت: دربارهٔ ما، تماس، قوانین، حریم خصوصی، ارسال و بازگشت
    public class PagesController : Controller
    {
        public ActionResult About() { return View(); }
        public ActionResult Contact() { return View(); }
        public ActionResult Terms() { return View(); }
        public ActionResult Privacy() { return View(); }
        public ActionResult Shipping() { return View(); }
    }
}
