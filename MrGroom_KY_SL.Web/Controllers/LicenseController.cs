using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Web.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [SkipLicenseCheck]
    public class LicenseController : Controller
    {
        private readonly LicenseService _licenseService = new LicenseService();

        [HttpGet]
        public ActionResult Activate()
        {
            var license = _licenseService.GetActiveLicense();

            ViewBag.IsActive = license != null && license.ExpiryDate >= DateTime.UtcNow;
            ViewBag.Expiry = license?.ExpiryDate;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activate(string productKey)
        {
            if (_licenseService.ActivateLicense(productKey))
            {
                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "License activated successfully!";

                return RedirectToAction("Login", "Account");
            }

            ViewBag.Error = "Invalid product key";

            var license = _licenseService.GetActiveLicense();
            ViewBag.IsActive = license != null && license.ExpiryDate >= DateTime.UtcNow;
            ViewBag.Expiry = license?.ExpiryDate;

            return View();
        }
    }
}