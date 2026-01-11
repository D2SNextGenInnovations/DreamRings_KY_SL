using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Common;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using MrGroom_KY_SL.Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [NoCache]
    public class AdminController : Controller
    {
        private readonly UserService _userService = new UserService();
        private readonly DashboardService _dashboardService = new DashboardService();

        public ActionResult Index(string searchTerm, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 5;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var users = _userService.GetAll();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    users = users.Where(u =>
                        (u.Username != null && u.Username.ToLower().Contains(searchTerm)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchTerm))
                    );
                }

                int totalUsers = users.Count();
                int totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

                var pageUsers = users
                    .OrderByDescending(u => u.CreatedOn)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        FirstName = u.FirstName,
                        CreatedOn = u.CreatedOn
                    })
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;

                if (Request.IsAjaxRequest())
                    return PartialView("_UsersTable", pageUsers);

                return View(pageUsers);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error fetching users: " + ex.Message;
                TempData["ToastrType"] = "error";
                return View(new List<UserViewModel>());
            }
        }

        public ActionResult AdminDashboard()
        {
            try
            {
                var model = new AdminDashboardViewModel
                {
                    TotalBookings = _dashboardService.TotalBookings(),
                    TotalCustomers = _dashboardService.TotalCustomers(),
                    TotalRevenue = _dashboardService.TotalRevenue(),
                    PendingPayments = _dashboardService.PendingPayments(),
                    Users = _userService.GetAll()
                        .Select(u => new UserViewModel
                        {
                            UserId = u.UserId,
                            Username = u.Username,
                            Email = u.Email,
                            Role = u.Role,
                            FirstName = u.FirstName,
                            CreatedOn = u.CreatedOn
                        })
                        .OrderByDescending(u => u.CreatedOn)
                        .Take(5)
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error loading dashboard: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public JsonResult GetBookingsForCalendar()
        {
            try
            {
                var service = new BookingService();
                var bookings = service.GetAll()
                    .Include(b => b.Customer)
                    .Include(b => b.EventType)
                    .ToList()
                    .Select(b => new
                    {
                        id = b.BookingId,
                        title = $"{(b.Customer?.FirstName ?? "Unknown")} - {(b.EventType?.Name ?? "Event")}",
                        start = b.EventDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = b.EventDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        location = b.Location ?? "N/A",
                        status = b.Status ?? "Pending"
                    })
                    .ToList();

                return Json(bookings, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error loading bookings: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Create()
        {
            try
            {
                return View(new UserViewModel());
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error initializing create form: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                if (_userService.GetByUsername(model.Username) != null)
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    return View(model);
                }

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Username = model.Username,
                    Password = PasswordHelper.HashPassword(model.Password),
                    Email = model.Email,
                    Gender = model.Gender,
                    Role = string.IsNullOrEmpty(model.Role) ? "User" : model.Role,
                    CreatedOn = DateTime.Now,
                    CreatedBy = model.Username,
                    IsActive = true
                };

                if (model.PhotoFile != null && model.PhotoFile.ContentLength > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        model.PhotoFile.InputStream.CopyTo(ms);
                        user.Photo = ms.ToArray();
                    }
                }

                _userService.Create(user);

                TempData["ToastrMessage"] = "User created successfully!";
                TempData["ToastrType"] = "success";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error creating user: " + ex.Message;
                TempData["ToastrType"] = "error";
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            try
            {
                var u = _userService.GetById(id);
                if (u == null)
                {
                    TempData["ToastrMessage"] = "User not found!";
                    TempData["ToastrType"] = "warning";
                    return RedirectToAction("Index");
                }

                var vm = new UserViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Gender = u.Gender,
                    Email = u.Email,
                    Role = u.Role,
                    PhotoBase64 = u.Photo != null ? Convert.ToBase64String(u.Photo) : null,
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = u.Username,
                    IsActive = u.IsActive
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error loading user for edit: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var u = _userService.GetById(model.UserId);
                if (u == null)
                {
                    TempData["ToastrMessage"] = "User not found!";
                    TempData["ToastrType"] = "warning";
                    return RedirectToAction("Index");
                }

                u.Username = model.Username;
                if (!string.IsNullOrEmpty(model.Password))
                {
                    u.Password = PasswordHelper.HashPassword(model.Password);
                }

                u.FirstName = model.FirstName;
                u.LastName = model.LastName;
                u.Gender = model.Gender;
                u.Email = model.Email;
                u.Role = model.Role;
                u.ModifiedOn = DateTime.Now;
                u.ModifiedBy = model.Username;
                u.IsActive = model.IsActive;

                if (model.PhotoFile != null && model.PhotoFile.ContentLength > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        model.PhotoFile.InputStream.CopyTo(ms);
                        u.Photo = ms.ToArray();
                    }
                }

                _userService.Update(u);

                TempData["ToastrMessage"] = "User updated successfully!";
                TempData["ToastrType"] = "success";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error updating user: " + ex.Message;
                TempData["ToastrType"] = "error";
                return View(model);
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                var u = _userService.GetById(id);
                if (u == null)
                {
                    TempData["ToastrMessage"] = "User not found!";
                    TempData["ToastrType"] = "warning";
                    return RedirectToAction("Index");
                }

                var vm = new UserViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error loading user for deletion: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _userService.Delete(id);
                TempData["ToastrMessage"] = "User deleted successfully!";
                TempData["ToastrType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error deleting user: " + ex.Message;
                TempData["ToastrType"] = "error";
            }

            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
        {
            try
            {
                var user = _userService.GetById(id);
                if (user == null)
                {
                    TempData["ToastrMessage"] = "User not found.";
                    TempData["ToastrType"] = "warning";
                    return RedirectToAction("Index");
                }

                var viewModel = new UserViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Gender = user.Gender,
                    Email = user.Email,
                    Role = user.Role,
                    PhotoBase64 = user.Photo != null ? Convert.ToBase64String(user.Photo) : null,
                    CreatedOn = user.CreatedOn,
                    CreatedBy = user.CreatedBy,
                    ModifiedOn = user.ModifiedOn,
                    ModifiedBy = user.ModifiedBy,
                    IsActive = user.IsActive
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error loading user details: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }
    }
}