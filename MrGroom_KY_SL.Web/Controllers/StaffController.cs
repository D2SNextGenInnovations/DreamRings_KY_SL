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
    public class StaffController : Controller
    {
        private readonly StaffService _staffService = new StaffService();

        // GET: Staff
        public ActionResult Index(string searchTerm, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 5;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var staffList = _staffService.GetAll();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    staffList = staffList.Where(s =>
                        s.Name.ToLower().Contains(searchTerm) ||
                        (s.Email != null && s.Email.ToLower().Contains(searchTerm)) ||
                        (s.Role != null && s.Role.ToLower().Contains(searchTerm)) ||
                        (s.Phone != null && s.Phone.ToLower().Contains(searchTerm))
                    );
                }

                int totalItems = staffList.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pageData = staffList
                    .OrderBy(s => s.StaffId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;

                if (Request.IsAjaxRequest())
                    return PartialView("_StaffTable", pageData);

                return View(pageData);
            }
            catch
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to load staff members. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Create()
        {
            try
            {
                return View(new Staff());
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the staff creation form.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(Staff model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please fill in all required fields correctly.";
                    return View(model);
                }

                _staffService.Create(model);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Staff member created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An unexpected error occurred while creating the staff member.";
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            try
            {
                var staff = _staffService.GetById(id);
                if (staff == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Staff member not found.";
                    return RedirectToAction("Index");
                }

                return View(staff);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the edit form.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(Staff model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please correct the highlighted errors.";
                    return View(model);
                }

                _staffService.Update(model);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Staff details updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while updating the staff member.";
                return View(model);
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                var staff = _staffService.GetById(id);
                if (staff == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Staff member not found.";
                    return RedirectToAction("Index");
                }

                return View(staff);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the delete confirmation page.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _staffService.Delete(id);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Staff member deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while deleting the staff member.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult Details(int id)
        {
            try
            {
                var staff = _staffService.GetByIdWithBookings(id);

                if (staff == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Staff member not found.";
                    return RedirectToAction("Index");
                }

                return View(staff);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load staff details.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult ExportExcel()
        {
            try
            {
                var staffList = _staffService.GetAll()
                                .OrderBy(s => s.StaffId)
                                .ToList();

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Staff");

                    // Header row
                    string[] headers = { "ID", "Name", "Role", "Phone", "Email", "Created On", "Status" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[1, i + 1].Value = headers[i];
                        ws.Cells[1, i + 1].Style.Font.Bold = true;
                        ws.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        ws.Cells[1, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }

                    int row = 2;
                    foreach (var s in staffList)
                    {
                        ws.Cells[row, 1].Value = s.StaffId;
                        ws.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[row, 2].Value = s.Name;
                        ws.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 3].Value = s.Role;
                        ws.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 4].Value = s.Phone;
                        ws.Cells[row, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 5].Value = s.Email;
                        ws.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 6].Value = s.CreatedAt.ToString("dd MMM yyyy");
                        ws.Cells[row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 7].Value = s.IsActive == true ? "Active" : "Inactive";
                        ws.Cells[row, 7].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                        using (var range = ws.Cells[row, 1, row, 7])
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
                        $"Staff_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to export Excel: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public ActionResult ExportPdf()
        {
            var staffList = _staffService.GetAll().OrderBy(s => s.StaffId).ToList();

            using (var stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(pdfDoc, stream);

                pdfDoc.Open();

                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 5f, 25f, 15f, 15f, 25f, 10f, 5f });

                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                // Header row
                string[] headers = { "ID", "Name", "Role", "Phone", "Email", "Created On", "Status" };

                foreach (var h in headers)
                {
                    table.AddCell(new PdfPCell(new Phrase(h, headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 4
                    });
                }

                // Data rows
                foreach (var s in staffList)
                {
                    table.AddCell(new PdfPCell(new Phrase(s.StaffId.ToString(), bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER
                    });

                    table.AddCell(new PdfPCell(new Phrase(s.Name ?? "", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(s.Role ?? "", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(s.Phone ?? "", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(s.Email ?? "", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(s.CreatedAt.ToString("dd MMM yyyy"), bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(s.IsActive == true ? "Active" : "Inactive", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER
                    });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", $"Staff_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
        }

    }
}