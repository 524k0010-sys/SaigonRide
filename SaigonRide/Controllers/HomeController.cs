using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SaigonRide.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SetLanguage(string lang, string returnUrl)
        {
            var selectedLanguage = string.Equals(lang, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
            var cookie = new HttpCookie("sr_lang", selectedLanguage)
            {
                Expires = DateTime.UtcNow.AddYears(1),
                HttpOnly = false,
                Path = "/"
            };

            Response.Cookies.Add(cookie);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
