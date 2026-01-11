using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Common;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using MrGroom_KY_SL.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MrGroom_KY_SL.Web.Controllers
{
    [NoCache]
    public class AccountController : Controller
    {
        private readonly UserService _userService = new UserService();
        private readonly AccountService _accountService = new AccountService();

        [HttpGet]
        public ActionResult Login()
        {
            try
            {
                return View();
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to load the login page. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please fill in all required fields correctly.";
                    return View(model);
                }

                var user = _accountService.ValidateLogin(model.Username, model.Password);

                if (user != null)
                {
                    // Create authentication ticket
                    var authTicket = new FormsAuthenticationTicket(
                        1,
                        user.Username,
                        DateTime.Now,
                        DateTime.Now.AddMinutes(60),
                        model.RememberMe,
                        user.Role,
                        "/"
                    );

                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    if (model.RememberMe)
                        cookie.Expires = DateTime.Now.AddDays(7);

                    Response.Cookies.Add(cookie);

                    // Store in session
                    Session["User"] = new SessionUser
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Role = user.Role
                    };

                    TempData["ToastrType"] = "success";
                    TempData["ToastrMessage"] = "Welcome back, " + user.Username + "!";

                    if (user.Role == "Admin")
                        return RedirectToAction("AdminDashboard", "Admin");

                    return RedirectToAction("Index", "Home");
                }

                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Invalid username or password. Please try again.";
                return View(model);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An unexpected error occurred while logging in. Please try again later.";
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Register()
        {
            try
            {
                return View(new UserViewModel());
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the registration form. Please try again later.";
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(UserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please complete all required fields correctly.";
                    return View(model);
                }

                var existingUser = _userService.GetByUsername(model.Username);
                if (existingUser != null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Username already taken. Please choose another.";
                    return View(model);
                }

                var currentUser = Session["User"] as SessionUser;

                var newUser = new User
                {
                    Username = model.Username,
                    Password = PasswordHelper.HashPassword(model.Password),
                    Email = model.Email,
                    Role = string.IsNullOrEmpty(model.Role) ? "User" : model.Role,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = currentUser?.Username ?? model.Username
                };

                _userService.Create(newUser);

                Session["User"] = new SessionUser
                {
                    UserId = newUser.UserId,
                    Username = newUser.Username,
                    Role = newUser.Role
                };

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Account created successfully! Welcome " + newUser.Username + ".";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An unexpected error occurred while registering. Please try again later.";
                return View(model);
            }
        }

        public ActionResult Logout()
        {
            try
            {
                // Clear session and cookies
                Session.Clear();
                Session.Abandon();

                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                {
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName)
                    {
                        Expires = DateTime.Now.AddDays(-1)
                    };
                    Response.Cookies.Add(cookie);
                }

                FormsAuthentication.SignOut();

                // Prevent caching
                Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                Response.Cache.SetValidUntilExpires(false);
                Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetNoStore();

                TempData["ToastrType"] = "info";
                TempData["ToastrMessage"] = "You have been logged out successfully.";

                return RedirectToAction("Login", "Account");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred during logout. Please try again.";
                return RedirectToAction("Login", "Account", new { t = DateTime.UtcNow.Ticks });
            }
        }
    }
}