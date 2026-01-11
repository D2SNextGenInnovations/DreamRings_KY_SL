using iTextSharp.text;
using iTextSharp.text.pdf;
using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using OfficeOpenXml;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace MrGroom_KY_SL.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [NoCache]
    public class AdminPackageItemsController : Controller
    {
        private readonly PackageItemService _packageItemService = new PackageItemService();
        private readonly PackageService _packageService = new PackageService();

        public ActionResult Index(string searchTerm, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 5;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var items = _packageItemService.GetAll();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    items = items.Where(i =>
                        i.Name.ToLower().Contains(searchTerm) ||
                        (i.Description != null && i.Description.ToLower().Contains(searchTerm))
                    );
                }

                int totalItems = items.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pageItems = items
                    .OrderBy(i => i.PackageItemId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Always set ViewBags
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;

                // AJAX request → partial only
                if (Request.IsAjaxRequest())
                    return PartialView("_PackageItemsTable", pageItems);

                // Normal request → full view
                return View(pageItems);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while loading package items.";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Create()
        {
            try
            {
                return View(new PackageItem());
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to load the create form. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(PackageItem item)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please fill in all required fields correctly.";
                    return View(item);
                }

                _packageItemService.Add(item);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Package item added successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An unexpected error occurred while creating the item.";
                return View(item);
            }
        }

        public ActionResult Edit(int id)
        {
            try
            {
                var item = _packageItemService.GetById(id);
                if (item == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Package item not found.";
                    return RedirectToAction("Index");
                }

                var packages = _packageService.GetAll();
                ViewBag.Packages = new SelectList(packages, "PackageId", "Name", item.PackageItemId);

                return View(item);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the edit form. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(PackageItem item)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var packages = _packageService.GetAll();
                    ViewBag.Packages = new SelectList(packages, "PackageId", "Name", item.PackageItemId);

                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please correct the highlighted errors.";
                    return View(item);
                }

                _packageItemService.Update(item);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Package item updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while updating the item.";
                return View(item);
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                var item = _packageItemService.GetById(id);
                if (item == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Package item not found.";
                    return RedirectToAction("Index");
                }

                return View(item);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load delete confirmation. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _packageItemService.Delete(id);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Package item deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while deleting the item.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult Details(int id)
        {
            try
            {
                // Use eager loading for the related packages
                var item = _packageItemService.GetById(id, includeProperties: "Packages");

                if (item == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Package item not found.";
                    return RedirectToAction("Index");
                }

                return View(item);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load package item details.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult ExportExcel()
        {
            try
            {
                var items = _packageItemService.GetAll()
                                .OrderBy(i => i.PackageItemId)
                                .ToList();

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("PackageItems");

                    // Header row
                    ws.Cells[1, 1].Value = "Name";
                    ws.Cells[1, 2].Value = "Description";
                    ws.Cells[1, 3].Value = "Price";
                    ws.Cells[1, 4].Value = "Status";

                    // Style header
                    using (var range = ws.Cells[1, 1, 1, 4])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                        // Border for header
                        range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }

                    // Data rows
                    int row = 2;
                    foreach (var item in items)
                    {
                        ws.Cells[row, 1].Value = item.Name;
                        ws.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 2].Value = item.Description;
                        ws.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 3].Value = item.Price;
                        ws.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 4].Value = item.IsActive == true ? "Active" : "Inactive";
                        ws.Cells[row, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                        // Add borders to each row
                        using (var range = ws.Cells[row, 1, row, 4])
                        {
                            range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }

                        row++;
                    }

                    ws.Cells.AutoFitColumns();

                    var bytes = package.GetAsByteArray();

                    return File(bytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "PackageItems.xlsx");
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
            var items = _packageItemService.GetAll().OrderBy(i => i.PackageItemId).ToList();

            using (var stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(pdfDoc, stream);

                pdfDoc.Open();

                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 30f, 40f, 15f, 15f });

                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                table.AddCell(new PdfPCell(new Phrase("Name", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Description", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Price", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Status", headerFont)) {
                    HorizontalAlignment = Element.ALIGN_CENTER
                });

                foreach (var item in items)
                {
                    table.AddCell(new PdfPCell(new Phrase(item.Name, bodyFont)) {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });
                    table.AddCell(new PdfPCell(new Phrase(item.Description ?? "", bodyFont)) {
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

                return File(stream.ToArray(), "application/pdf", "PackageItems.pdf");
            }
        }

    }
}