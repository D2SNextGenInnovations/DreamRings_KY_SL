using iTextSharp.text;
using iTextSharp.text.pdf;
using MrGroom_KY_SL.Business.CustomExceptions;
using MrGroom_KY_SL.Business.DTOs;
using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using MrGroom_KY_SL.Web.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [NoCache]
    public class AdminBookingsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly BookingService _bookingService = new BookingService();
        private readonly StaffService _staffService = new StaffService();
        private readonly EventTypeService _eventTypeService = new EventTypeService();
        private readonly PackageService _packageService = new PackageService();
        private readonly CustomerService _customerService = new CustomerService();
        private readonly PaymentService _paymentService = new PaymentService();

        public ActionResult Index(string searchTerm, string status = null, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 10;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var bookingsQuery = _bookingService.GetAll();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    bookingsQuery = bookingsQuery.Where(b =>
                        (b.Customer.FirstName + " " + b.Customer.LastName).ToLower().Contains(searchTerm) ||
                        (b.Package != null && b.Package.Name.ToLower().Contains(searchTerm)) ||
                        (b.Status != null && b.Status.ToLower().Contains(searchTerm))
                    );
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    bookingsQuery = bookingsQuery.Where(b => b.Status == status);
                }

                // Summary counts
                ViewBag.TotalBookings = bookingsQuery.Count();
                ViewBag.ConfirmedCount = bookingsQuery.Count(b => b.Status == "Confirmed");
                ViewBag.PendingCount = bookingsQuery.Count(b => b.Status == "Pending");
                ViewBag.CancelledCount = bookingsQuery.Count(b => b.Status == "Cancelled");

                // Pagination
                int totalItems = bookingsQuery.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pageItems = bookingsQuery
                    .OrderByDescending(b => b.BookingDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;
                ViewBag.StatusFilter = status;

                // Return partial view for AJAX requests
                if (Request.IsAjaxRequest())
                    return PartialView("_BookingsTable", pageItems);

                return View(pageItems);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Error fetching bookings: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Create()
        {
            try
            {
                PopulateDropdowns();
                PopulatePackageViewBags(null);
                return View(new BookingCreateViewModel());
            }
            catch
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the booking creation form.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BookingCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                PopulatePackageViewBags(vm.PackageId);
                return View(vm);
            }

            var booking = new Booking
            {
                CustomerId = vm.CustomerId,
                PackageId = vm.PackageId.Value,
                EventDate = vm.EventDate,
                Location = vm.Location,
                SelectedEventTypeIds = vm.SelectedEventTypeIds
            };

            var addonDtos = vm.SelectedAddons?
                .Where(a => a.Quantity > 0)
                .Select(a => new BookingAddonDTO
                {
                    PackageItemId = a.PackageItemId,
                    Quantity = a.Quantity
                })
                .ToList();


            _bookingService.Create(booking, vm.SelectedStaffIds, addonDtos);

            return RedirectToAction("Edit", new { id = booking.BookingId });
        }

        private void PopulatePackageViewBags(int? packageId)
        {
            if (!packageId.HasValue)
            {
                ViewBag.PackageEventTypeIds = new HashSet<int>();
                ViewBag.PackageItemIds = new HashSet<int>();
                ViewBag.PackageItemQuantities = new Dictionary<int, int>();
                return;
            }

            var package = _packageService.GetByIdWithDetails(packageId.Value);
            if (package == null) return;

            ViewBag.PackageEventTypeIds = package.PackageEventTypes
                .Select(p => p.EventTypeId)
                .ToHashSet();

            ViewBag.PackageItemIds = package.PackageItemPackages
                .Select(p => p.PackageItemId)
                .ToHashSet();

            ViewBag.PackageItemQuantities = package.PackageItemPackages
                .ToDictionary(p => p.PackageItemId, p => p.Qty);
        }

        [HttpGet]
        public JsonResult GetBookingSummary(int id)
        {
            var booking = _bookingService
                .GetAll() // IQueryable<Booking>
                .Include(b => b.Customer)
                .Include(b => b.Package)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
                return Json(new { success = false, message = "Booking not found" }, JsonRequestBehavior.AllowGet);

            var lastPayment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();

            return Json(new
            {
                success = true,
                bookingId = booking.BookingId,
                customerName = booking.Customer.FirstName + " " + (booking.Customer.LastName ?? ""),
                fullAmount = booking.Package?.BasePrice ?? 0,
                prevPaid = booking.Payments.Sum(p => p.Amount),
                paymentMethod = lastPayment?.PaymentMethod ?? "",
                paymentType = lastPayment?.PaymentType ?? "",
                remarks = lastPayment?.Remarks ?? ""
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateAjax(BookingCreateViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Validation failed." });

            try
            {
                Booking savedBooking;

                //edit
                if (vm.BookingId > 0)
                {
                    var booking = new Booking
                    {
                        BookingId = vm.BookingId,
                        CustomerId = vm.CustomerId,
                        PackageId = vm.PackageId.Value,
                        EventDate = vm.EventDate,
                        Location = vm.Location,
                        SelectedEventTypeIds = vm.SelectedEventTypeIds
                    };

                    var addonDtos = vm.SelectedAddons?
                        .Where(a => a.Quantity > 0)
                        .Select(a => new BookingAddonDTO
                        {
                            PackageItemId = a.PackageItemId,
                            Quantity = a.Quantity
                        })
                        .ToList();

                    _bookingService.Update(booking, vm.SelectedStaffIds, addonDtos);
                    savedBooking = _bookingService.GetById(vm.BookingId);
                }
                else
                {
                    // Create new booking
                    var booking = new Booking
                    {
                        CustomerId = vm.CustomerId,
                        PackageId = vm.PackageId.Value,
                        EventDate = vm.EventDate,
                        Location = vm.Location,
                        SelectedEventTypeIds = vm.SelectedEventTypeIds
                    };

                    var addonDtos = vm.SelectedAddons?
                        .Where(a => a.Quantity > 0)
                        .Select(a => new BookingAddonDTO
                        {
                            PackageItemId = a.PackageItemId,
                            Quantity = a.Quantity
                        })
                        .ToList();

                    savedBooking = _bookingService.Create(booking, vm.SelectedStaffIds, addonDtos);
                }

                // Reload booking with relations
                var fullBooking = _bookingService.GetAll()
                    .Include(b => b.Payments)
                    .Include(b => b.Package.PackageEventTypes.Select(pet => pet.EventType))
                    .Include(b => b.Package.PackageItemPackages)
                    .Include(b => b.BookingAddons.Select(a => a.PackageItem))
                    .Include(b => b.BookingEventTypes.Select(be => be.EventType))
                    .FirstOrDefault(b => b.BookingId == savedBooking.BookingId);

                if (fullBooking == null)
                    return Json(new { success = false, message = "Booking not found after save." });

                var customer = _customerService.GetById(fullBooking.CustomerId);
                string customerName = customer != null
                    ? $"{customer.FirstName} {customer.LastName}"
                    : "Unknown";

                decimal packageAmount = fullBooking.Package?.BasePrice ?? 0m;
                decimal eventTypesAmount = fullBooking.BookingEventTypes.Sum(be => be.EventType.Price);
                decimal addonsTotal = fullBooking.BookingAddons.Sum(a => a.Quantity * a.UnitPrice);
                decimal fullAmount = packageAmount + eventTypesAmount + addonsTotal;
                decimal prevPaid = fullBooking.Payments?.Sum(p => p.Amount) ?? 0m;
                decimal balance = fullAmount - prevPaid;

                return Json(new
                {
                    success = true,
                    unpaid = balance > 0,
                    isFullyPaid = balance <= 0,
                    bookingId = fullBooking.BookingId,
                    message = vm.BookingId > 0
                        ? "Booking updated successfully!"
                        : "Booking created successfully!",

                    customerName,
                    packageAmount,
                    eventTypesAmount,
                    addonsAmount = addonsTotal,
                    totalAddonsAmount = eventTypesAmount + addonsTotal,
                    fullAmount,
                    prevPaid,
                    balance,

                    eventTypes = fullBooking.BookingEventTypes
                        .GroupBy(x => x.EventType)
                        .Select(g => new
                        {
                            name = g.Key.Name,
                            qty = g.Count(),
                            price = g.Key.Price,
                            total = g.Count() * g.Key.Price
                        }),

                    addons = fullBooking.BookingAddons
                        .Where(a => a.Quantity > 0)
                        .GroupBy(a => new
                        {
                            a.PackageItemId,
                            a.PackageItem.Name,
                            a.UnitPrice
                        })
                        .Select(g => new
                        {
                            name = g.Key.Name,
                            qty = g.Sum(x => x.Quantity),
                            price = g.Key.UnitPrice,
                            total = g.Sum(x => x.Quantity * g.Key.UnitPrice)
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error saving booking: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public ActionResult Edit(int id)
        {
            var booking = _bookingService.GetById(id);
            if (booking == null)
                return HttpNotFound();

            // Package items included in package
            var packageItemIds = booking.Package?.PackageItemPackages
                .Select(p => p.PackageItemId)
                .ToHashSet() ?? new HashSet<int>();

            // Package event types
            var packageEventTypeIds = booking.Package?.PackageEventTypes
                .Select(p => p.EventTypeId)
                .ToHashSet() ?? new HashSet<int>();

            // Selected event type IDs = package + extra
            var selectedEventTypeIds = packageEventTypeIds
                .Union(booking.BookingEventTypes.Select(be => be.EventTypeId))
                .ToArray();

            // Selected Addons = package items + extra addons
            var selectedAddons = new List<BookingAddonViewModel>();

            // Add package items first
            // In Edit action
            foreach (var pip in booking.Package?.PackageItemPackages ?? Enumerable.Empty<PackageItemPackage>())
            {
                // Check if a BookingAddon exists for this package item
                var existingAddon = booking.BookingAddons.FirstOrDefault(a => a.PackageItemId == pip.PackageItemId);

                selectedAddons.Add(new BookingAddonViewModel
                {
                    PackageItemId = pip.PackageItemId,
                    Quantity = existingAddon?.Quantity ?? pip.Qty //use actual package quantity
                });
            }

            // Add extra addons not in package
            foreach (var extra in booking.BookingAddons.Where(a => !packageItemIds.Contains(a.PackageItemId)))
            {
                selectedAddons.Add(new BookingAddonViewModel
                {
                    PackageItemId = extra.PackageItemId,
                    Quantity = extra.Quantity
                });
            }

            // Build ViewModel
            var vm = new BookingCreateViewModel
            {
                BookingId = booking.BookingId,
                CustomerId = booking.CustomerId,
                PackageId = booking.PackageId,
                EventDate = booking.EventDate == DateTime.MinValue ? null : booking.EventDate,
                Location = booking.Location,
                Notes = booking.Notes,
                Status = booking.Status,
                SelectedStaffIds = booking.StaffMembers.Select(s => s.StaffId).ToArray(),
                SelectedEventTypeIds = booking.BookingEventTypes.Select(be => be.EventTypeId).ToArray(),
                SelectedAddons = booking.BookingAddons
          .Select(a => new BookingAddonViewModel
          {
              PackageItemId = a.PackageItemId,
              Quantity = a.Quantity
          })
          .ToList()
            };

            PopulateDropdowns(vm, vm.SelectedStaffIds);
            PopulatePackageViewBags(vm.PackageId);
            ModelState.Remove("EventDate");
            return View("Create", vm); // Reuse the same view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(BookingCreateViewModel model, int[] SelectedStaffIds, List<BookingAddonDTO> SelectedAddons)
        {
            try
            {
                // Normalize inputs
                if (SelectedAddons == null)
                {
                    SelectedAddons = new List<BookingAddonDTO>();
                }

                var existingBooking = _bookingService.GetById(model.BookingId);
                if (existingBooking == null)
                    return Json(new { success = false, message = "Booking not found." });

                // --- STEP 1: Detect cancel first ---
                bool wasCancelled = existingBooking.Status == "Cancelled";
                bool isCanceled = Request.Form["IsCanceled"] != null;
                bool wasReactivated = false;

                // Restore staff for disabled select
                SelectedStaffIds = SelectedStaffIds ?? model.SelectedStaffIds;

                if (isCanceled)
                {
                    if (existingBooking.Status != "Cancelled")
                        existingBooking.PreviousStatus = existingBooking.Status;

                    existingBooking.Status = "Cancelled";

                    _bookingService.Update(existingBooking, SelectedStaffIds, null);

                    return Json(new
                    {
                        success = true,
                        bookingId = existingBooking.BookingId,
                        message = "Booking has been canceled."
                    });
                }

                // --- Reactivate if cancelled---
                if (wasCancelled && !isCanceled)
                {
                    existingBooking.Status = !string.IsNullOrEmpty(existingBooking.PreviousStatus)
                        ? existingBooking.PreviousStatus
                        : "Pending";

                    existingBooking.PreviousStatus = null;
                    wasReactivated = true;
                }

                if ((SelectedStaffIds == null || SelectedStaffIds.Length == 0) &&
                    model.SelectedStaffIds != null)
                {
                    SelectedStaffIds = model.SelectedStaffIds;
                }

                // Validations
                if (!ModelState.IsValid)
                {
                    PopulateDropdowns(model, SelectedStaffIds);

                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "Please fill in all required fields correctly." });

                    return View(model);
                }

                // Load booking
                //var existingBooking = _bookingService.GetById(model.BookingId);
                if (existingBooking == null)
                {
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "Booking not found." });

                    return HttpNotFound();
                }

                // Update base fields
                existingBooking.CustomerId = model.CustomerId;
                existingBooking.PackageId = model.PackageId.Value;
                existingBooking.BookingDate = model.BookingDate;
                existingBooking.EventDate = model.EventDate;
                existingBooking.Location = model.Location;
                existingBooking.Notes = model.Notes;
                existingBooking.SelectedEventTypeIds = model.SelectedEventTypeIds;

                // Save booking
                _bookingService.Update(existingBooking, SelectedStaffIds, SelectedAddons);

                // Reload booking with relations
                var fullBooking = _bookingService.GetAll()
                    .Include(b => b.Payments)
                    .Include(b => b.Customer)
                    .Include(b => b.Package.PackageEventTypes)
                    .Include(b => b.Package.PackageItemPackages)
                    .Include(b => b.BookingEventTypes.Select(x => x.EventType))
                    //.Include(b => b.BookingAddons)
                    .Include(b => b.BookingAddons.Select(a => a.PackageItem))
                    .FirstOrDefault(b => b.BookingId == existingBooking.BookingId);

                if (fullBooking == null)
                    throw new Exception("Failed to reload booking.");

                //AMOUNT CALCULATION 

                // Package
                decimal packageAmount = fullBooking.Package?.BasePrice ?? 0m;

                // Event types
                var packageEventTypeCountMap = fullBooking.Package?.PackageEventTypes
                    .GroupBy(x => x.EventTypeId)
                    .ToDictionary(g => g.Key, g => g.Count())
                    ?? new Dictionary<int, int>();

                var selectedEventTypeCountMap = fullBooking.BookingEventTypes
                    .GroupBy(x => x.EventTypeId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var eventTypePriceMap = fullBooking.BookingEventTypes
                    .GroupBy(x => x.EventTypeId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().EventType.Price
                    );

                decimal eventTypesAmount = 0m;

                foreach (var kv in selectedEventTypeCountMap)
                {
                    int eventTypeId = kv.Key;
                    int selectedCount = kv.Value;

                    int pkgCnt;
                    int packageCount = packageEventTypeCountMap.TryGetValue(eventTypeId, out pkgCnt)
                        ? pkgCnt
                        : 0;

                    int chargeableCount = selectedCount - packageCount;

                    if (chargeableCount <= 0)
                        continue;

                    decimal price;
                    if (!eventTypePriceMap.TryGetValue(eventTypeId, out price))
                        continue;

                    eventTypesAmount += chargeableCount * price;
                }

                var packageItemQtyMap = fullBooking.Package?.PackageItemPackages
                    .ToDictionary(x => x.PackageItemId, x => x.Qty)
                    ?? new Dictionary<int, int>();

                decimal addonsAmount = 0m;

                foreach (var a in fullBooking.BookingAddons)
                {
                    int packageQty = packageItemQtyMap.ContainsKey(a.PackageItemId)
                        ? packageItemQtyMap[a.PackageItemId]
                        : 0;

                    int chargeableQty = a.Quantity - packageQty;

                    if (chargeableQty > 0)
                    {
                        addonsAmount += chargeableQty * a.UnitPrice;
                    }
                }

                // Total
                decimal fullAmount = fullBooking.GrandTotal;
                decimal totalPaid = fullBooking.Payments?.Sum(p => p.Amount) ?? 0m;
                decimal balance = fullBooking.RemainingAmount;

                fullBooking.Status = fullBooking.RemainingAmount <= 0 ? "Confirmed" : "Pending";
                //fullBooking.Status = balance <= 0 ? "Confirmed" : "Pending";

                var eventTypeSummary = fullBooking.BookingEventTypes
                 .GroupBy(x => x.EventType)
                 .Select(g => new
                 {
                     name = g.Key.Name,
                     qty = g.Count(),
                     price = g.Key.Price,
                     total = g.Count() * g.Key.Price
                 })
                 .ToList();

                var addonSummary = fullBooking.BookingAddons
                        .Select(a =>
                        {
                            int packageQty = packageItemQtyMap.ContainsKey(a.PackageItemId)
                                ? packageItemQtyMap[a.PackageItemId]
                                : 0;

                            int chargeableQty = Math.Max(0, a.Quantity - packageQty);

                            return new
                            {
                                a.PackageItemId,
                                Name = a.PackageItem.Name,
                                UnitPrice = a.UnitPrice,
                                ChargeableQty = chargeableQty
                            };
                        })
                        .Where(x => x.ChargeableQty > 0)
                        .GroupBy(x => new { x.PackageItemId, x.Name, x.UnitPrice })
                        .Select(g => new
                        {
                            name = g.Key.Name,
                            qty = g.Sum(x => x.ChargeableQty),
                            price = g.Key.UnitPrice,
                            total = g.Sum(x => x.ChargeableQty * x.UnitPrice)
                        })
                        .ToList();



                // AJAX response
                if (Request.IsAjaxRequest())
                {
                    var lastPayment = fullBooking.Payments?
                        .OrderByDescending(p => p.PaymentId)
                        .FirstOrDefault();

                    string message;

                    if (isCanceled)
                        message = "Booking has been canceled.";
                    else if (wasReactivated)
                        message = "Booking has been reactivated.";
                    else
                        message = "Booking updated successfully.";

                    return Json(new
                    {
                        success = true,
                        unpaid = fullBooking.RemainingAmount > 0,
                        isFullyPaid = fullBooking.RemainingAmount <= 0,
                        bookingId = fullBooking.BookingId,
                        customerName = fullBooking.Customer != null ? $"{fullBooking.Customer.FirstName} {fullBooking.Customer.LastName}" : "",
                        packageAmount,
                        eventTypesAmount,
                        addonsAmount,
                        totalAddonsAmount = addonsAmount + eventTypesAmount,
                        fullAmount,
                        prevPaid = totalPaid,
                        balance,
                        eventTypes = eventTypeSummary,
                        addons = addonSummary,
                        paymentMethod = lastPayment?.PaymentMethod ?? "",
                        paymentType = lastPayment?.PaymentType ?? "",
                        remarks = lastPayment?.Remarks ?? ""
                    });

                }

                // Normal redirect
                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Booking updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Error updating booking: " + ex.Message });

                ModelState.AddModelError("", "Error updating booking: " + ex.Message);
                PopulateDropdowns(model, SelectedStaffIds);
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            try
            {
                var booking = _bookingService.GetById(id);
                if (booking == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Booking not found.";
                    return RedirectToAction("Index");
                }
                return View(booking);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = $"Error loading booking for deletion: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _bookingService.Delete(id);
                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Booking deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = $"Error deleting booking: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private void PopulateDropdowns(BookingCreateViewModel model = null, int[] selectedStaff = null)
        {
            var customers = _customerService.GetAll()
                .Select(c => new
                {
                    c.CustomerId,
                    Name = c.FirstName + " " + (c.LastName ?? "")
                })
                .ToList();

            ViewBag.Customers = new SelectList(customers, "CustomerId", "Name", model?.CustomerId);
            ViewBag.Packages = new SelectList(_packageService.GetAll(), "PackageId", "Name", model?.PackageId);

            ViewBag.PackageItems = _uow.PackageItemRepository.GetAll().Where(p => p.IsActive == true).ToList();
            ViewBag.EventTypes = _eventTypeService.GetAll().Where(e => e.IsActive == true).ToList();

            var allStaff = _staffService.GetAll().ToList();
            ViewBag.StaffList = new MultiSelectList(allStaff, "StaffId", "Name", selectedStaff);
        }

        // SAVE PAYMENT (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SavePayment(Payment payment)
        {
            try
            {
                if (payment == null)
                    return Json(new { success = false, message = "Payment is missing." });

                // SAFELY parse Amount using invariant culture
                if (payment.Amount <= 0)
                {
                    decimal parsedAmount = 0;
                    decimal.TryParse(Request.Form["Amount"], NumberStyles.Any, CultureInfo.InvariantCulture, out parsedAmount);
                    payment.Amount = parsedAmount;
                }

                if (payment.Amount <= 0)
                    return Json(new { success = false, message = "Invalid payment amount." });

                // Validate BookingId
                if (payment.BookingId <= 0)
                {
                    int bookingIdFromForm = 0;
                    int.TryParse(Request.Form["BookingId"], out bookingIdFromForm);
                    payment.BookingId = bookingIdFromForm;
                }

                if (payment.BookingId <= 0)
                    return Json(new { success = false, message = "BookingId is missing." });

                // Load booking
                var booking = _bookingService.GetById(payment.BookingId);
                if (booking == null)
                    return Json(new { success = false, message = "Booking not found." });

                decimal remaining = booking.RemainingAmount;

                if (payment.Amount > remaining)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Payment amount exceeds remaining balance."
                    });
                }

                if (booking.Status == "Canceled")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Payments are not allowed for canceled bookings."
                    });
                }

                // Ensure payment is linked to booking
                payment.BookingId = booking.BookingId;
                payment.Booking = null;

                // Set PaymentDate if not already set
                if (payment.PaymentDate == default(DateTime))
                    payment.PaymentDate = DateTime.UtcNow;

                // Save payment
                _paymentService.AddPayment(payment);

                var updatedBooking = _bookingService.GetAll().Include(b => b.Payments).FirstOrDefault(b => b.BookingId == payment.BookingId);

                decimal totalPaid = updatedBooking.Payments.Sum(p => p.Amount);
                string status = totalPaid >= 10000 ? "Confirmed" : "Pending";

                _bookingService.UpdateStatus(updatedBooking.BookingId, status);

                // Return success with invoice URL
                return Json(new
                {
                    success = true,
                    message = "Payment saved!",
                    invoiceUrl = Url.Action("GenerateInvoice", new { id = payment.BookingId })
                });
            }
            catch (Exception ex)
            {
                // Log exception (you can replace with proper logging)
                System.Diagnostics.Debug.WriteLine(ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        private Booking GetFullBooking(int id)
        {
            return _bookingService.GetAll()
                .Include(b => b.Customer)
                .Include(b => b.Payments)
                .Include(b => b.Package)
                .Include(b => b.Package.PackageItemPackages.Select(p => p.PackageItem))
                .Include(b => b.Package.PackageEventTypes.Select(p => p.EventType))
                .Include(b => b.BookingAddons.Select(a => a.PackageItem))
                .Include(b => b.BookingEventTypes.Select(be => be.EventType))
                .FirstOrDefault(b => b.BookingId == id);
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            try
            {
                var booking = _bookingService.GetById(id.Value);

                if (booking == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Booking not found.";
                    return RedirectToAction("Index");
                }

                // Related details
                ViewBag.Customer = _customerService.GetById(booking.CustomerId);
                ViewBag.Package = _packageService.GetById(booking.PackageId);

                // EventTypes
                ViewBag.EventTypes = booking.BookingEventTypes?.Select(x => x.EventType).ToList() ?? new List<EventType>();

                ViewBag.Staff = booking.StaffMembers?.ToList() ?? new List<Staff>();
                ViewBag.Payments = _paymentService.GetAllByBooking(booking.BookingId);

                return View(booking);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = $"Error loading booking details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public ActionResult ExportExcel()
        {
            try
            {
                var bookings = _bookingService.GetAll().OrderBy(b => b.BookingId).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Bookings");

                    string[] headers = {
                        "ID",
                        "Customer",
                        "Phone",
                        "Email",
                        "Package",
                        "Base Price",
                        "Event Types",
                        "Location",
                        "Event Date",
                        "Booking Date",
                        "Status",
                        "Notes",
                        "Total Paid",
                        "Remaining",
                        "Payment Status",
                        "Assigned Staff"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[1, i + 1].Value = headers[i];
                        ws.Cells[1, i + 1].Style.Font.Bold = true;
                        ws.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    int row = 2;

                    foreach (var b in bookings)
                    {
                        ws.Cells[row, 1].Value = b.BookingId;
                        ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        ws.Cells[row, 2].Value = b.Customer?.FirstName + " " + b.Customer?.LastName;
                        ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 3].Value = b.Customer?.Phone;
                        ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 4].Value = b.Customer?.Email;
                        ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 5].Value = b.Package?.Name;
                        ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 6].Value = b.Package?.BasePrice ?? 0;
                        ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 7].Value = string.Join(", ", b.BookingEventTypes?.Select(be => be.EventType?.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n)) ?? Enumerable.Empty<string>());
                        ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 8].Value = b.Location;
                        ws.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 9].Value = b.EventDate.HasValue && b.EventDate.Value > DateTime.MinValue ? b.EventDate.Value.ToString("yyyy-MM-dd") : "";

                        ws.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 10].Value = b.BookingDate.ToString("yyyy-MM-dd");
                        ws.Cells[row, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 11].Value = b.Status;
                        ws.Cells[row, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        ws.Cells[row, 12].Value = b.Notes;
                        ws.Cells[row, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 13].Value = b.TotalPaid;
                        ws.Cells[row, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 14].Value = b.RemainingAmount;
                        ws.Cells[row, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 15].Value = b.PaymentStatus;
                        ws.Cells[row, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        ws.Cells[row, 16].Value = string.Join(", ", b.StaffMembers?.Select(s => s.Name) ?? new List<string>());
                        ws.Cells[row, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        row++;
                    }

                    // Apply borders
                    using (var range = ws.Cells[1, 1, row - 1, headers.Length])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }

                    ws.Cells.AutoFitColumns();

                    return File(package.GetAsByteArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Bookings.xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to export Excel file: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public ActionResult ExportPdf()
        {
            try
            {
                var bookings = _bookingService.GetAll().OrderBy(b => b.BookingId).ToList();

                using (var stream = new MemoryStream())
                {
                    Document pdfDoc = new Document(PageSize.A4.Rotate(), 10f, 10f, 20f, 20f);
                    PdfWriter.GetInstance(pdfDoc, stream);

                    pdfDoc.Open();

                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                    Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                    PdfPTable table = new PdfPTable(15);
                    table.WidthPercentage = 100;

                    string[] headers = {
                "Customer", "Phone", "Email", "Package", "Base Price",
                "Event Type", "Location", "Event Date", "Booking Date",
                "Status", "Notes", "Total Paid", "Remaining",
                "Payment Status", "Assigned Staff"
            };

                    foreach (string h in headers)
                    {
                        table.AddCell(new PdfPCell(new Phrase(h, headerFont))
                        {
                            BackgroundColor = new BaseColor(235, 235, 235),
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                    }

                    foreach (var b in bookings)
                    {
                        table.AddCell(new Phrase(b.Customer?.FirstName + " " + b.Customer?.LastName, bodyFont));
                        table.AddCell(new Phrase(b.Customer?.Phone, bodyFont));
                        table.AddCell(new Phrase(b.Customer?.Email, bodyFont));
                        table.AddCell(new Phrase(b.Package?.Name, bodyFont));
                        table.AddCell(new Phrase((b.Package?.BasePrice ?? 0).ToString("N2"), bodyFont));

                        table.AddCell(new Phrase(string.Join(", ", b.BookingEventTypes?.Select(be => be.EventType?.Name).Where(n => !string.IsNullOrWhiteSpace(n)) ?? Enumerable.Empty<string>()), bodyFont));

                        table.AddCell(new Phrase(b.Location, bodyFont));
                        table.AddCell(new Phrase(b.EventDate.HasValue && b.EventDate.Value > DateTime.MinValue ? b.EventDate.Value.ToString("dd/MM/yyyy") : "", bodyFont));

                        table.AddCell(new Phrase(b.BookingDate.ToString("dd/MM/yyyy"), bodyFont));
                        table.AddCell(new Phrase(b.Status, bodyFont));
                        table.AddCell(new Phrase(b.Notes, bodyFont));
                        table.AddCell(new Phrase(b.TotalPaid.ToString("N2"), bodyFont));
                        table.AddCell(new Phrase(b.RemainingAmount.ToString("N2"), bodyFont));
                        table.AddCell(new Phrase(b.PaymentStatus, bodyFont));

                        table.AddCell(new Phrase(
                            string.Join(", ", b.StaffMembers?.Select(s => s.Name) ?? new List<string>()),
                            bodyFont));
                    }

                    pdfDoc.Add(table);
                    pdfDoc.Close();

                    return File(stream.ToArray(), "application/pdf", "Bookings.pdf");
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to export PDF: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public FileResult GenerateInvoice(int id)
        {
            var booking = GetFullBooking(id);
            if (booking == null)
                return null;

            byte[] pdf = InvoicePdfGenerator.GenerateInvoice(booking);

            return File(pdf, "application/pdf", $"Invoice_{id}.pdf");
        }

        [HttpGet]
        public JsonResult GetPackageAddons(int packageId)
        {
            var package = _packageService.GetByIdWithDetails(packageId);

            if (package == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,

                eventTypes = package.PackageEventTypes.Select(pet => new
                {
                    eventTypeId = pet.EventTypeId,
                    name = pet.EventType.Name,
                    price = pet.EventType.Price
                }),

                items = package.PackageItemPackages.Select(pip => new
                {
                    packageItemId = pip.PackageItemId,
                    name = pip.PackageItem.Name,
                    quantity = pip.Qty
                })
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult SendWhatsAppClickToChat(int id)
        {
            var booking = GetFullBooking(id);
            if (booking == null || booking.Customer == null)
                return HttpNotFound();

            // Format Sri Lanka phone number → 94
            string phone = booking.Customer.Phone ?? string.Empty;
            phone = phone.Replace(" ", "").Replace("-", "").Replace("+", "");

            if (phone.StartsWith("0"))
                phone = "94" + phone.Substring(1);

            // Safety check
            if (phone.Length < 11)
                return new HttpStatusCodeResult(400, "Invalid phone number");

            // Calculate amounts
            decimal packageAmount = booking.Package?.BasePrice ?? 0m;
            decimal eventTypesAmount = booking.BookingEventTypes?.Sum(e => e.EventType.Price) ?? 0m;
            decimal addonsAmount = booking.BookingAddons?.Sum(a => a.Quantity * a.UnitPrice) ?? 0m;

            decimal total = packageAmount + eventTypesAmount + addonsAmount;
            decimal paid = booking.Payments?.Sum(p => p.Amount) ?? 0m;
            decimal balance = total - paid;

            // message
            string message =
                "Booking Confirmed\n\n" +
                "Customer: " + booking.Customer.FirstName + " " + booking.Customer.LastName + "\n" +
                //"Event Date: " + booking.EventDate.ToString("yyyy-MM-dd") + "\n" +
                "Event Date: " + (booking.EventDate.HasValue && booking.EventDate.Value > DateTime.MinValue ? booking.EventDate.Value.ToString("yyyy-MM-dd") : "N/A") + "\n" +

                "Location: " + booking.Location + "\n\n" +

                "Package: " + booking.Package?.Name + "\n" +
                "Package Price: Rs " + packageAmount.ToString("N2") + "\n\n" +

                "Event Types:\n" +
                (booking.BookingEventTypes != null && booking.BookingEventTypes.Any()
                    ? string.Join("\n", booking.BookingEventTypes
                        .GroupBy(e => e.EventType.Name)
                        .Select(g => "- " + g.Key + " x" + g.Count()))
                    : "None") +
                "\n\n" +

                "Addons:\n" +
                (booking.BookingAddons != null && booking.BookingAddons.Any()
                    ? string.Join("\n", booking.BookingAddons.Select(a =>
                        "- " + a.PackageItem.Name + " x" + a.Quantity +
                        " (Rs " + a.UnitPrice.ToString("N2") + ")"))
                    : "None") +
                "\n\n" +

                "Payment Summary\n" +
                "Total: Rs " + total.ToString("N2") + "\n" +
                "Paid: Rs " + paid.ToString("N2") + "\n" +
                "Balance: Rs " + balance.ToString("N2") + "\n\n" +
                "Dream Rings Photography";

            // Encode + Redirect 
            string encodedMessage = HttpUtility.UrlEncode(message);
            string url = $"https://wa.me/{phone}?text={encodedMessage}";

            return Redirect(url);
        }
    }
}
