using MrGroom_KY_SL.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MrGroom_KY_SL.Web.Filters;

namespace MrGroom_KY_SL.Web.Controllers
{
    [NoCache]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                var user = (SessionUser)Session["User"];
                if (user == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "You must log in to continue.";
                    return RedirectToAction("Login", "Account");
                }

                // Redirect based on role
                if (user.Role == "Admin")
                {
                    return RedirectToAction("AdminDashboard");
                }
                else if (user.Role == "User")
                {
                    return RedirectToAction("UserDashboard");
                }

                // Default fallback
                TempData["ToastrType"] = "info";
                TempData["ToastrMessage"] = "Welcome to the home page.";
                return View();
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An unexpected error occurred while loading the home page.";
                return RedirectToAction("Login", "Account");
            }
        }

        [CustomAuthorize(Role = "Admin")]
        public ActionResult AdminDashboard()
        {
            try
            {
                var user = (SessionUser)Session["User"];
                if (user == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please log in to access the admin dashboard.";
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.Username = user.Username;

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Welcome to your Admin Dashboard!";
                return View();
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while loading the Admin Dashboard. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        [CustomAuthorize(Role = "User")]
        public ActionResult UserDashboard()
        {
            try
            {
                var user = (SessionUser)Session["User"];
                if (user == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please log in to access your dashboard.";
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.Username = user.Username;

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Welcome to your User Dashboard!";
                return View();
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while loading your dashboard.";
                return RedirectToAction("Index");
            }
        }

        [CustomAuthorize]
        public ActionResult UserProfile()
        {
            try
            {
                var user = (SessionUser)Session["User"];
                if (user == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please log in to view your profile.";
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.Username = user.Username;
                ViewBag.Role = user.Role;

                TempData["ToastrType"] = "info";
                TempData["ToastrMessage"] = "Here’s your profile information.";
                return View();
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Index");
            }
        }
    }
}