using iTextSharp.text;
using iTextSharp.text.pdf;
using MrGroom_KY_SL.Business.CustomExceptions;
using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
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

        public ActionResult Index(string searchTerm, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 10;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var bookings = _bookingService.GetAll();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    bookings = bookings.Where(b =>
                        (b.Customer.FirstName + " " + b.Customer.LastName).ToLower().Contains(searchTerm) ||
                        (b.Package != null && b.Package.Name.ToLower().Contains(searchTerm)) ||
                        (b.Status != null && b.Status.ToLower().Contains(searchTerm))
                    );
                }

                int totalItems = bookings.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pageItems = bookings
                    .OrderByDescending(b => b.BookingDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;

                if (Request.IsAjaxRequest())
                    return PartialView("_BookingsTable", pageItems);

                return View(pageItems);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = $"Error loading bookings: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Create()
        {
            try
            {
                PopulateDropdowns();
                return View(new Booking());
            }
            catch
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the booking creation form.";
                return RedirectToAction("Index");
            }
        }

        // CREATE POST 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Booking model, int[] SelectedStaffIds)
        {
            if (SelectedStaffIds == null || SelectedStaffIds.Length == 0)
                ModelState.AddModelError("SelectedStaffIds", "Please assign at least one staff member.");

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model, SelectedStaffIds);
                return View(model);
            }

            try
            {
                _bookingService.Create(model, SelectedStaffIds);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Booking created successfully!";
                return RedirectToAction("Edit", new { id = model.BookingId });
            }
            catch (ExistingUnpaidBookingWarningException ex)
            {
                TempData["ToastrType"] = "warning";
                TempData["ToastrMessage"] = ex.Message + " Please complete the pending payment.";
                return RedirectToAction("Edit", new { id = ex.ExistingBookingId });
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = ex.Message;

                PopulateDropdowns(model, SelectedStaffIds);
                return View(model);
            }
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
        public JsonResult CreateAjax(Booking model, int[] SelectedStaffIds)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Validation failed." });
            }

            try
            {
                // 1. Check for existing unpaid booking
                var unpaidBooking = _bookingService.GetAll()
                    .Include(b => b.Payments)
                    .Where(b => b.CustomerId == model.CustomerId)
                    .ToList() // Important: EF cannot evaluate custom logic
                    .Where(b => b.TotalPaid > b.Payments.Sum(p => p.Amount)) // unpaid or partially paid
                    .FirstOrDefault();

                if (unpaidBooking != null)
                {
                    return Json(new
                    {
                        unpaid = true,
                        bookingId = unpaidBooking.BookingId,
                        message = "This customer already has an unpaid booking."
                    });
                }

                // 2. Create booking AND GET THE REAL ID
                var savedBooking = _bookingService.Create(model, SelectedStaffIds);

                // 3. Reload booking with package + payments
                var booking = _bookingService.GetAll()
                    .Include(b => b.Payments)
                    .Include(b => b.Package)
                    .FirstOrDefault(b => b.BookingId == savedBooking.BookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found after creation." });
                }

                // 4. Get customer info
                var customer = _customerService.GetById(booking.CustomerId);
                string customerName = customer != null
                    ? $"{customer.FirstName} {customer.LastName}"
                    : "Unknown";

                // 5. Calculate amounts
                decimal fullAmount = booking.Package?.BasePrice ?? 0m;
                decimal prevPaid = booking.TotalPaid;

                return Json(new
                {
                    success = true,
                    bookingId = booking.BookingId,
                    message = "Booking created successfully!",
                    customerName = customerName,
                    fullAmount = fullAmount,
                    prevPaid = prevPaid
                });
            }
            catch (ExistingUnpaidBookingWarningException ex)
            {
                return Json(new
                {
                    success = false,
                    unpaid = true,
                    bookingId = ex.ExistingBookingId,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Edit(int id)
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

                // Map staff selections
                booking.SelectedStaffIds = booking.StaffMembers?.Select(s => s.StaffId).ToArray() ?? new int[0];

                // Populate dropdowns with selected values
                PopulateDropdowns(booking, booking.SelectedStaffIds);

                // Format date for HTML5 input
                ViewBag.EventDateFormatted = booking.EventDate.ToString("yyyy-MM-dd");

                ViewBag.Payments = _paymentService.GetAllByBooking(id);
                return View(booking);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = $"Error loading booking for edit: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Booking model, int[] SelectedStaffIds)
        {
            try
            {
                if ((SelectedStaffIds == null || SelectedStaffIds.Length == 0) && model.SelectedStaffIds != null)
                {
                    SelectedStaffIds = model.SelectedStaffIds;
                }

                if (SelectedStaffIds == null || SelectedStaffIds.Length == 0)
                {
                    ModelState.AddModelError("SelectedStaffIds", "You must assign at least one staff member.");
                }

                if (!ModelState.IsValid)
                {
                    PopulateDropdowns(model, SelectedStaffIds);
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "Please fill in all required fields correctly." });
                    return View(model);
                }

                var existingBooking = _bookingService.GetById(model.BookingId);
                if (existingBooking == null)
                {
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "Booking not found." });
                    return HttpNotFound();
                }

                // Update booking details
                existingBooking.CustomerId = model.CustomerId;
                existingBooking.PackageId = model.PackageId;
                existingBooking.EventTypeId = model.EventTypeId;
                existingBooking.BookingDate = model.BookingDate;
                existingBooking.EventDate = model.EventDate;
                existingBooking.Notes = model.Notes;
                existingBooking.StaffMembers = _uow.StaffRepository.GetAll()
                    .Where(s => SelectedStaffIds.Contains(s.StaffId))
                    .ToList();

                bool isCanceled = Request.Form["IsCanceled"] == "true";

                if (isCanceled)
                {
                    existingBooking.Status = "Cancelled";

                    _bookingService.Update(existingBooking, SelectedStaffIds);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            bookingId = existingBooking.BookingId,
                            message = "Booking has been canceled."
                        });
                    }

                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Booking has been canceled.";
                    return RedirectToAction("Index");
                }

                // --- Check if payment is pending ---
                var totalPaid = existingBooking.Payments.Sum(p => p.Amount);
                var packagePrice = existingBooking.Package?.BasePrice ?? 0;
                var balance = packagePrice - totalPaid;

                existingBooking.Status = totalPaid >= 15000 ? "Confirm" : "Pending";

                _bookingService.Update(existingBooking, SelectedStaffIds);

                //// --- Check if payment is pending ---
                //var totalPaid = existingBooking.Payments.Sum(p => p.Amount);
                //var packagePrice = existingBooking.Package?.BasePrice ?? 0;
                //var balance = packagePrice - totalPaid;

                //existingBooking.Status = totalPaid >= 15000 ? "Confirm" : "Pending";

                if (Request.IsAjaxRequest())
                {
                    if (balance > 0)
                    {
                        var lastPayment = existingBooking.Payments
                            .OrderByDescending(p => p.PaymentId)
                            .FirstOrDefault();

                        return Json(new
                        {
                            success = true,
                            unpaid = true,
                            bookingId = existingBooking.BookingId,
                            message = "Booking updated! Please complete payment.",
                            customerName = existingBooking.Customer.FirstName + " " + (existingBooking.Customer.LastName ?? ""),
                            fullAmount = packagePrice,
                            prevPaid = totalPaid,
                            paymentMethod = lastPayment?.PaymentMethod ?? "",
                            paymentType = lastPayment?.PaymentType ?? "",
                            remarks = lastPayment?.Remarks ?? ""
                        });
                    }

                    return Json(new
                    {
                        success = true,
                        bookingId = existingBooking.BookingId,
                        message = "Booking updated successfully!"
                    });
                }

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Booking updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = $"Error updating booking: {ex.Message}" });

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

        private void PopulateDropdowns(Booking model = null, int[] selectedStaff = null)
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
            ViewBag.EventTypes = new SelectList(_eventTypeService.GetAll(), "EventTypeId", "Name", model?.EventTypeId);

            // Make sure staff list name matches View usage
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

                var updatedBooking = _bookingService.GetAll()
                    .Include(b => b.Payments)
                    .FirstOrDefault(b => b.BookingId == payment.BookingId);

                decimal totalPaid = updatedBooking.Payments.Sum(p => p.Amount);
                string status = totalPaid >= 15000 ? "Confirmed" : "Pending";

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
            return _bookingService.GetById(id);
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

                // Load related details
                ViewBag.Customer = _customerService.GetById(booking.CustomerId);
                ViewBag.Package = _packageService.GetById(booking.PackageId);
                ViewBag.EventType = _eventTypeService.GetById(booking.EventTypeId);
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
                        "Event Type",
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

                        ws.Cells[row, 7].Value = b.EventType?.Name;
                        ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 8].Value = b.Location;
                        ws.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 9].Value = b.EventDate.ToString("yyyy-MM-dd");
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

                        ws.Cells[row, 16].Value = string.Join(", ",b.StaffMembers?.Select(s => s.Name) ?? new List<string>());
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
                        table.AddCell(new Phrase(b.EventType?.Name, bodyFont));
                        table.AddCell(new Phrase(b.Location, bodyFont));
                        table.AddCell(new Phrase(b.EventDate.ToString("dd/MM/yyyy"), bodyFont));
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
    }
}
