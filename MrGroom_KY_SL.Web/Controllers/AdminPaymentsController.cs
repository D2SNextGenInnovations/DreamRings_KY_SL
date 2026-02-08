using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    public class AdminPaymentsController : Controller
    {
        private readonly PaymentService _paymentService = new PaymentService();

        [HttpGet]
        public ActionResult Payments(string customerName = null, int? bookingId = null, int page = 1)
        {
            int pageSize = 10;
            var paymentsQuery = _paymentService.GetAllPayments()
                .Include(p => p.Booking)
                .Include(p => p.Booking.Customer)
                .Include(p => p.Booking.Payments)
                .Include(p => p.Booking.Package)
                .Include(p => p.Booking.BookingAddons)
                .Include(p => p.Booking.BookingEventTypes.Select(e => e.EventType));

            // Filter by customer name
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                string lowerName = customerName.ToLower();
                paymentsQuery = paymentsQuery.Where(p =>
                    (p.Booking.Customer.FirstName + " " + (p.Booking.Customer.LastName ?? "")).ToLower()
                    .Contains(lowerName));
            }

            // Filter by booking ID
            if (bookingId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.BookingId == bookingId.Value);
            }

            //var customerTotals = paymentsQuery
            //    .GroupBy(p => p.Booking.Customer.CustomerId)
            //    .Select(g => new CustomerPaymentTotalVM
            //    {
            //        CustomerName = g.FirstOrDefault().Booking.Customer.FirstName + " " +
            //                       (g.FirstOrDefault().Booking.Customer.LastName ?? ""),
            //        TotalAmount = g.Sum(x => x.Amount),
            //        PaymentCount = g.Count()
            //    })
            //    .OrderByDescending(x => x.TotalAmount)
            //    .ToList();
            var customerTotals = paymentsQuery
                .GroupBy(p => p.Booking.Customer)
                .Select(g => new CustomerPaymentTotalVM
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.FirstName + " " + (g.Key.LastName ?? ""),
                    TotalAmount = g.Sum(x => x.Amount),
                    PaymentCount = g.Count(),
                    Payments = g
                        .OrderByDescending(x => x.PaymentDate)
                        .Select(x => new CustomerPaymentRowVM
                        {
                            PaymentDate = x.PaymentDate,
                            Amount = x.Amount,
                            Method = x.PaymentMethod,
                            Type = x.PaymentType,
                            Status = "Paid"
                        }).ToList()
                })
                .ToList();


            ViewBag.CustomerTotals = customerTotals;

            int totalItems = paymentsQuery.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pageItems = paymentsQuery
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Booking)
                .Include(p => p.Booking.Customer)
                .Include(p => p.Booking.Payments)
                .Include(p => p.Booking.Package)
                .Include(p => p.Booking.BookingEventTypes.Select(bet => bet.EventType))
                .Include(p => p.Booking.BookingAddons)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CustomerNameFilter = customerName;
            ViewBag.BookingIdFilter = bookingId;

            return View(pageItems);
        }
    }
}