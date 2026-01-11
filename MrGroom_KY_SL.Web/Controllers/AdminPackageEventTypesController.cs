using iTextSharp.text;
using iTextSharp.text.pdf;
using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [NoCache]
    public class AdminPackageEventTypesController : Controller
    {
        private readonly PackageEventTypeService _packageEventTypeService = new PackageEventTypeService();
        private readonly PackageService _packageService = new PackageService();
        private readonly EventTypeService _eventService = new EventTypeService();

        public ActionResult Index(string searchTerm, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 5;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var items = _eventService.GetAll();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    items = items.Where(i => i.Name.ToLower().Contains(searchTerm));
                }

                int totalItems = items.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pageItems = items
                    .OrderBy(i => i.EventTypeId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;

                if (Request.IsAjaxRequest())
                    return PartialView("_PackageEventTypesTable", pageItems);

                return View(pageItems);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Error fetching event types: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Create
        public ActionResult Create()
        {
            try
            {
                var model = new EventType
                {
                    IsActive = true
                };
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error initializing creation form: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // POST: Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(EventType model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                _eventService.Add(model);
                TempData["ToastrMessage"] = "Event type created successfully!";
                TempData["ToastrType"] = "success";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error creating event type: " + ex.Message;
                TempData["ToastrType"] = "error";
                return View(model);
            }
        }

        // GET: Edit
        public ActionResult Edit(int id)
        {
            try
            {
                var model = _eventService.GetById(id);
                if (model == null)
                {
                    TempData["ToastrMessage"] = "Event type not found!";
                    TempData["ToastrType"] = "warning";
                    return RedirectToAction("Index");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error fetching event type: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // POST: Edit
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(EventType model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                _eventService.Update(model);
                TempData["ToastrMessage"] = "Event type updated successfully!";
                TempData["ToastrType"] = "success";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error updating event type: " + ex.Message;
                TempData["ToastrType"] = "error";
                return View(model);
            }
        }

        // GET: Delete
        public ActionResult Delete(int id)
        {
            try
            {
                var model = _eventService.GetById(id);
                if (model == null)
                {
                    TempData["ToastrMessage"] = "Event type not found!";
                    TempData["ToastrType"] = "warning";
                    return RedirectToAction("Index");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error fetching event type: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // POST: Delete
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _eventService.Delete(id);
                TempData["ToastrMessage"] = "Event type deleted successfully!";
                TempData["ToastrType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error deleting event type: " + ex.Message;
                TempData["ToastrType"] = "error";
            }
            return RedirectToAction("Index");
        }

        // GET: Details
        public ActionResult Details(int id)
        {
            try
            {
                // find the Package-Event Type link first
                var model = _packageEventTypeService.GetByIdDetails(id);

                if (model != null)
                {
                    return View(model); // Found a package-event link, show as usual
                }

                // If not found, fall back to EventType only
                var eventType = _eventService.GetById(id);
                if (eventType != null)
                {
                    // Create a lightweight model so the same Details view can be reused
                    var fallbackModel = new PackageEventType
                    {
                        EventType = eventType,
                        Package = null
                    };
                    return View(fallbackModel);
                }

                TempData["ToastrMessage"] = "Event type not found!";
                TempData["ToastrType"] = "warning";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "Error fetching details: " + ex.Message;
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // ---------------------------
        // Export to Excel
        // ---------------------------
        public ActionResult ExportExcel()
        {
            try
            {
                var items = _eventService.GetAll().OrderBy(i => i.EventTypeId).ToList();

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("EventTypes");

                    // Set headers
                    ws.Cells[1, 1].Value = "Name";
                    ws.Cells[1, 2].Value = "Unit Price (Rs)";
                    ws.Cells[1, 3].Value = "Status";

                    // Header styling
                    using (var range = ws.Cells[1, 1, 1, 3])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }

                    // Fill data
                    int row = 2;
                    foreach (var item in items)
                    {
                        ws.Cells[row, 1].Value = item.Name;
                        ws.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 2].Value = item.Price;
                        ws.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 3].Value = item.IsActive == true ? "Active" : "Inactive";
                        ws.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                        // Add borders for each cell
                        for (int col = 1; col <= 3; col++)
                        {
                            var cell = ws.Cells[row, col];
                            cell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }

                        row++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string excelName = $"EventTypes-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to export Excel: " + ex.Message;
                return RedirectToAction("Index");
            }
        }


        // ---------------------------
        // Export to PDF
        // ---------------------------
        public ActionResult ExportPdf()
        {
            try
            {
                // Get all EventTypes ordered by EventTypeId
                var items = _eventService.GetAll().OrderBy(i => i.EventTypeId).ToList();

                using (var stream = new MemoryStream())
                {
                    // Create PDF document
                    Document pdfDoc = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                    PdfWriter.GetInstance(pdfDoc, stream);

                    pdfDoc.Open();

                    // Create table with 3 columns: Name, Price, Status
                    PdfPTable table = new PdfPTable(3);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 50f, 25f, 25f });

                    // Fonts
                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                    // Table headers
                    table.AddCell(new PdfPCell(new Phrase("Event Type", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Unit Price (Rs)", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Status", headerFont)){
                        HorizontalAlignment = Element.ALIGN_CENTER
                    });

                    // Table body
                    foreach (var item in items)
                    {
                        table.AddCell(new PdfPCell(new Phrase(item.Name, bodyFont)) {
                            HorizontalAlignment = Element.ALIGN_RIGHT
                        });
                        table.AddCell(new PdfPCell(new Phrase(item.Price.ToString("N2"), bodyFont)) {
                            HorizontalAlignment = Element.ALIGN_RIGHT
                        });
                        table.AddCell(new PdfPCell(new Phrase(item.IsActive == true ? "Active" : "Inactive", bodyFont)) {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                    }

                    pdfDoc.Add(table);
                    pdfDoc.Close();

                    return File(stream.ToArray(), "application/pdf", "EventTypes.pdf");
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to export PDF: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}