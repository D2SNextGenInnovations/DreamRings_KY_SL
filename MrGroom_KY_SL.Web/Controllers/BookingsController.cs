using MrGroom_KY_SL.Business.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly BookingService _booking = new BookingService();
        public ActionResult Index()
        {
            var list = _booking.GetAll().ToList();
            return View(list);
        }

        public ActionResult Details(int id)
        {
            var b = _booking.GetById(id);
            if (b == null) return HttpNotFound();
            return View(b);
        }
    }
}