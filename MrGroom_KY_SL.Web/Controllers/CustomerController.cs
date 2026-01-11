using iTextSharp.text;
using iTextSharp.text.pdf;
using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerService _customerService = new CustomerService();

        // get all customers
        public ActionResult Index(string searchTerm, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 5;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var customers = _customerService.GetAll();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    customers = customers.Where(c =>
                        c.FirstName.ToLower().Contains(searchTerm) ||
                        (!string.IsNullOrEmpty(c.LastName) && c.LastName.ToLower().Contains(searchTerm)) ||
                        (!string.IsNullOrEmpty(c.Email) && c.Email.ToLower().Contains(searchTerm)) ||
                        (!string.IsNullOrEmpty(c.Phone) && c.Phone.ToLower().Contains(searchTerm))
                    );
                }

                int totalItems = customers.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pageCustomers = customers
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;

                if (Request.IsAjaxRequest())
                    return PartialView("_CustomersTable", pageCustomers);

                return View(pageCustomers);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Error loading customers.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        // POST
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(CustomerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Please fill in required fields correctly.";
                    return View(model);
                }

                var customer = new Customer
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    NICNumber = model.NICNumber,
                    CreatedAt = DateTime.UtcNow
                };

                // address
                var address = new CustomerAddress
                {
                    AddressLine1 = model.AddressLine1,
                    AddressLine2 = model.AddressLine2,
                    AddressLine3 = model.AddressLine3,
                    City = model.City,
                    StateOrProvince = model.StateOrProvince,
                    PostalCode = model.PostalCode,
                    Country = model.Country,
                    AddressType = model.AddressType,
                    IsPrimary = true,
                    CreatedDate = DateTime.Now
                };

                customer.Addresses = new List<CustomerAddress> { address };

                _customerService.Add(customer);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Customer created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to create customer.";
                return View(model);
            }
        }

        // Edit GET
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var customer = _customerService.GetById(id);
            if (customer == null)
            {
                TempData["ToastrType"] = "warning";
                TempData["ToastrMessage"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            var address = customer.Addresses?.FirstOrDefault();

            var model = new CustomerViewModel
            {
                CustomerId = customer.CustomerId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                NICNumber = customer.NICNumber,
                AddressLine1 = address?.AddressLine1,
                AddressLine2 = address?.AddressLine2,
                AddressLine3 = address?.AddressLine3,
                City = address?.City,
                StateOrProvince = address?.StateOrProvince,
                PostalCode = address?.PostalCode,
                Country = address?.Country,
                AddressType = address?.AddressType,
            };

            return View(model);
        }

        // Edit POST
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ToastrType"] = "warning";
                TempData["ToastrMessage"] = "Please correct the errors.";
                return View(model);
            }

            try
            {
                var existing = _customerService.GetById(model.CustomerId);
                if (existing == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Customer not found.";
                    return RedirectToAction("Index");
                }

                // Update main customer fields
                existing.FirstName = model.FirstName;
                existing.LastName = model.LastName;
                existing.Email = model.Email;
                existing.Phone = model.Phone;
                existing.NICNumber = model.NICNumber;

                // Handle address 
                var address = existing.Addresses?.FirstOrDefault(a => a.IsPrimary)
                              ?? existing.Addresses?.FirstOrDefault();

                if (address != null)
                {
                    // Update existing address (no FK issue)
                    address.AddressLine1 = model.AddressLine1;
                    address.AddressLine2 = model.AddressLine2;
                    address.AddressLine3 = model.AddressLine3;
                    address.City = model.City;
                    address.StateOrProvince = model.StateOrProvince;
                    address.PostalCode = model.PostalCode;
                    address.Country = model.Country;
                    address.AddressType = model.AddressType;
                    address.IsPrimary = true;
                    address.UpdatedDate = DateTime.Now;
                }
                else
                {
                    // Add a new address if none exists
                    address = new CustomerAddress
                    {
                        AddressLine1 = model.AddressLine1,
                        AddressLine2 = model.AddressLine2,
                        AddressLine3 = model.AddressLine3,
                        City = model.City,
                        StateOrProvince = model.StateOrProvince,
                        PostalCode = model.PostalCode,
                        Country = model.Country,
                        AddressType = model.AddressType,
                        IsPrimary = true,
                        CreatedDate = DateTime.Now,
                        CustomerId = existing.CustomerId
                    };
                    existing.Addresses = new List<CustomerAddress> { address };
                }

                _customerService.Update(existing);

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Customer updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Error updating customer: " + ex.Message;
                return View(model);
            }
        }


        // Details
        public ActionResult Details(int id)
        {
            var customer = _customerService.GetById(id);
            if (customer == null)
            {
                TempData["ToastrType"] = "warning";
                TempData["ToastrMessage"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            return View(customer);
        }

        // Delete GET
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            var customer = _customerService.GetById(id);
            if (customer == null)
            {
                TempData["ToastrType"] = "warning";
                TempData["ToastrMessage"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            return View(customer);
        }

        // Delete POST
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _customerService.Delete(id);
                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Customer deleted successfully.";
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Error deleting customer.";
            }

            return RedirectToAction("Index");
        }

        public ActionResult ExportExcel()
        {
            try
            {
                var customers = _customerService.GetAll()
                                .OrderBy(c => c.CustomerId)
                                .ToList();

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Customers");

                    // Header row
                    string[] headers = { "ID", "Name", "Email", "Phone No.", "NIC Number", "Address" };
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
                    foreach (var cust in customers)
                    {
                        ws.Cells[row, 1].Value = cust.CustomerId;
                        ws.Cells[row, 2].Value = $"{cust.FirstName} {cust.LastName}".Trim();
                        ws.Cells[row, 3].Value = cust.Email;
                        ws.Cells[row, 4].Value = cust.Phone;
                        ws.Cells[row, 5].Value = cust.NICNumber;

                        // Concatenate full address if multiple addresses exist
                        string fullAddress = string.Join(", ",
                            cust.Addresses.Select(a =>
                                $"{a.AddressLine1} {a.AddressLine2} {a.AddressLine3}, {a.City}, {a.StateOrProvince}, {a.PostalCode}, {a.Country}"
                            ).Where(s => !string.IsNullOrEmpty(s))
                        );
                        ws.Cells[row, 6].Value = fullAddress;

                        // Add borders for each cell
                        using (var range = ws.Cells[row, 1, row, 6])
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
                                $"Customers_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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
            var customers = _customerService.GetAll()
                                .OrderBy(c => c.CustomerId)
                                .ToList();

            using (var stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(pdfDoc, stream);

                pdfDoc.Open();

                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 5f, 15f, 20f, 15f, 15f, 30f });

                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                // Header row
                string[] headers = { "ID", "Name", "Email", "Phone No.", "NIC Number", "Address" };
                foreach (var h in headers)
                {
                    table.AddCell(new PdfPCell(new Phrase(h, headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER
                    });
                }

                // Data rows
                foreach (var cust in customers)
                {
                    // ID - center aligned
                    table.AddCell(new PdfPCell(new Phrase(cust.CustomerId.ToString(), bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER
                    });

                    // Other fields - right aligned
                    table.AddCell(new PdfPCell(new Phrase($"{cust.FirstName} {cust.LastName}".Trim(), bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(cust.Email ?? "", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(cust.Phone ?? "", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    table.AddCell(new PdfPCell(new Phrase(cust.NICNumber ?? "", bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });

                    string fullAddress = string.Join(", ",
                        cust.Addresses.Select(a =>
                            $"{a.AddressLine1} {a.AddressLine2} {a.AddressLine3}, {a.City}, {a.StateOrProvince}, {a.PostalCode}, {a.Country}"
                        ).Where(s => !string.IsNullOrWhiteSpace(s))
                    );

                    table.AddCell(new PdfPCell(new Phrase(fullAddress, bodyFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", $"Customers_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
        }
    }
}