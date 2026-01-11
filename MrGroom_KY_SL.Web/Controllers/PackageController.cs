using iTextSharp.text;
using iTextSharp.text.pdf;
using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using MrGroom_KY_SL.Web.Models;
using OfficeOpenXml;
using SendGrid;
using SendGrid.Helpers.Mail;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [NoCache]
    public class PackageController : Controller
    {
        private readonly PackageService _packageService = new PackageService();
        private readonly PackageItemService _packageItemService = new PackageItemService();
        private readonly EventTypeService _eventTypeService = new EventTypeService();

        // GET: Package
        public ActionResult Index(string searchTerm, int page = 1, string manage = null)
        {
            try
            {
                int pageSize = 5;
                bool isManageMode = !string.IsNullOrEmpty(manage);

                var packages = _packageService.GetAll();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    packages = packages.Where(p =>
                        p.Name.ToLower().Contains(searchTerm) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchTerm))
                    );
                }

                int totalItems = packages.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pageData = packages
                    .OrderBy(p => p.PackageId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsManageMode = isManageMode;

                if (Request.IsAjaxRequest())
                    return PartialView("_PackageTable", pageData);

                return View(pageData);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to load packages. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public ActionResult Create()
        {
            try
            {
                var model = new PackageViewModel
                {
                    PackageItemsFull = _packageItemService.GetAll() ?? new List<PackageItem>(),
                    PackageEventsFull = _eventTypeService.GetAll() ?? new List<EventType>(),

                    // Initialize empty selections
                    SelectedPackageItems = new List<int>(),
                    SelectedEventTypeIds = new List<int>()
                };

                return View(model);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load the create form. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(PackageViewModel model)
        {
            try
            {
                // If validation failed, reload dropdowns and return
                if (!ModelState.IsValid)
                    return ReloadViewWithDropdowns(model, "Please fill in all required fields correctly.", "warning");

                using (var uow = new UnitOfWork())
                {
                    var package = new Package
                    {
                        Name = model.Name,
                        Description = model.Description,
                        BasePrice = model.BasePrice,
                        DurationHours = model.DurationHours,
                        IsActive = model.IsActive,
                        IsIncludeSodftCopies = model.IsIncludeSodftCopies,
                        EditedPhotosCount = model.EditedPhotosCount
                    };

                    // ADD SELECTED PACKAGE ITEMS + QTY
                    if (model.SelectedPackageItemDetails != null)
                    {
                        foreach (var itemVM in model.SelectedPackageItemDetails.Where(x => x.Qty > 0))
                        {
                            var dbItem = uow.PackageItemRepository.GetById(itemVM.PackageItemId);
                            if (dbItem == null) continue;

                            var total = dbItem.Price * itemVM.Qty;

                            package.PackageItemPackages.Add(new PackageItemPackage
                            {
                                PackageItemId = dbItem.PackageItemId,
                                Qty = itemVM.Qty,
                                UnitPrice = itemVM.UnitPrice,
                                CalculatedPrice = total
                            });
                        }
                    }

                    // ADD SELECTED EVENT TYPES
                    if (model.SelectedEventTypeIds != null)
                    {
                        foreach (var evtId in model.SelectedEventTypeIds)
                        {
                            var dbEvt = uow.EventTypeRepository.GetById(evtId);
                            if (dbEvt == null) continue;

                            package.PackageEventTypes.Add(new PackageEventType
                            {
                                EventTypeId = evtId,
                                UnitPrice = dbEvt.Price
                            });
                        }
                    }

                    // HANDLE UPLOADED PHOTOS
                    SaveUploadedPhotos(package, model.UploadedPhotos);

                    // SAVE
                    uow.PackageRepository.Insert(package);
                    uow.Save();
                }

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Package created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An unexpected error occurred while creating the package.";
                return RedirectToAction("Index");
            }
        }

        private ActionResult ReloadViewWithDropdowns(PackageViewModel model, string message, string toastrType)
        {
            model.PackageItemsFull = _packageItemService.GetAll() ?? new List<PackageItem>();
            model.PackageEventsFull = _eventTypeService.GetAll() ?? new List<EventType>();

            model.PackageItems = model.PackageItemsFull.Select(i => new SelectListItem
            {
                Value = i.PackageItemId.ToString(),
                Text = i.Name,
                Selected = model.SelectedPackageItems != null &&
                           model.SelectedPackageItems.Contains(i.PackageItemId)
            });

            model.EventTypes = model.PackageEventsFull.Select(e => new SelectListItem
            {
                Value = e.EventTypeId.ToString(),
                Text = e.Name,
                Selected = model.SelectedEventTypeIds != null &&
                           model.SelectedEventTypeIds.Contains(e.EventTypeId)
            });

            TempData["ToastrType"] = toastrType;
            TempData["ToastrMessage"] = message;

            return View("Create", model);
        }

        private void SaveUploadedPhotos(Package package, List<HttpPostedFileBase> uploadedPhotos)
        {
            if (uploadedPhotos == null || !uploadedPhotos.Any())
                return;

            var uploadDir = Server.MapPath("~/Uploads/PackagePhotos");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            int displayOrder = 0;

            foreach (var file in uploadedPhotos)
            {
                if (file == null || file.ContentLength == 0)
                    continue;

                if (displayOrder >= 3)
                    break;

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var path = Path.Combine(uploadDir, fileName);
                file.SaveAs(path);

                package.PackagePhotos.Add(new PackagePhoto
                {
                    PhotoUrl = "/Uploads/PackagePhotos/" + fileName,
                    DisplayOrder = ++displayOrder
                });
            }
        }

        public ActionResult Details(int id)
        {
            try
            {
                var package = _packageService.GetByIdWithDetails(id);

                if (package == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Package not found.";
                    return RedirectToAction("Index");
                }

                return View(package);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load package details.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var package = _packageService.GetById(
                id,
                includeProperties: "PackageItemPackages.PackageItem,PackageEventTypes.EventType,PackagePhotos"
            );

            if (package == null)
                return HttpNotFound();

            var model = new PackageViewModel
            {
                PackageId = package.PackageId,
                Name = package.Name,
                Description = package.Description,
                BasePrice = package.BasePrice,
                DurationHours = package.DurationHours,
                IsActive = package.IsActive,
                IsIncludeSodftCopies = package.IsIncludeSodftCopies,
                EditedPhotosCount = package.EditedPhotosCount,

                // THIS IS CRUCIAL: map existing package items to your selection VM
                SelectedPackageItemDetails = package.PackageItemPackages
                    .Select(p => new PackageItemSelectionVM
                    {
                        PackageItemId = p.PackageItemId,
                        Qty = p.Qty,
                        UnitPrice = p.UnitPrice
                    }).ToList(),

                // Selected event types
                SelectedEventTypeIds = package.PackageEventTypes
                    .Select(e => e.EventTypeId)
                    .ToList(),

                // Full lists for the checkboxes
                PackageItemsFull = _packageItemService.GetAll().ToList(),
                PackageEventsFull = _eventTypeService.GetAll().ToList()
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(PackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.PackageItemsFull = _packageItemService.GetAll();
                model.PackageEventsFull = _eventTypeService.GetAll();
                return View(model);
            }

            using (var uow = new UnitOfWork())
            {
                var package = uow.PackageRepository.Get(
                    filter: p => p.PackageId == model.PackageId,
                    includeProperties: "PackageItemPackages,PackageEventTypes,PackagePhotos"
                ).FirstOrDefault();

                if (package == null)
                {
                    TempData["ToastrType"] = "error";
                    TempData["ToastrMessage"] = "Package not found.";
                    return RedirectToAction("Index");
                }

                // Update basic fields
                package.Name = model.Name;
                package.Description = model.Description;
                package.DurationHours = model.DurationHours;
                package.IsActive = model.IsActive;
                package.IsIncludeSodftCopies = model.IsIncludeSodftCopies;
                package.EditedPhotosCount = model.EditedPhotosCount;

                // -------------------------
                // UPDATE PACKAGE ITEMS
                // -------------------------
                // Delete old items properly
                foreach (var oldItem in package.PackageItemPackages.ToList())
                {
                    uow.Context.Set<PackageItemPackage>().Remove(oldItem);
                }

                // Add new items
                if (model.SelectedPackageItemDetails != null)
                {
                    foreach (var item in model.SelectedPackageItemDetails.Where(x => x.Qty > 0))
                    {
                        var dbItem = uow.PackageItemRepository.GetById(item.PackageItemId);
                        if (dbItem == null) continue;

                        package.PackageItemPackages.Add(new PackageItemPackage
                        {
                            PackageItemId = item.PackageItemId,
                            Qty = item.Qty,
                            UnitPrice = dbItem.Price,
                            CalculatedPrice = dbItem.Price * item.Qty
                        });
                    }
                }

                // -------------------------
                // UPDATE EVENT TYPES
                // -------------------------
                foreach (var oldEvent in package.PackageEventTypes.ToList())
                {
                    uow.Context.Set<PackageEventType>().Remove(oldEvent);
                }

                foreach (var evtId in model.SelectedEventTypeIds ?? new List<int>())
                {
                    package.PackageEventTypes.Add(new PackageEventType
                    {
                        EventTypeId = evtId
                    });
                }

                // -------------------------
                // UPDATE PHOTOS
                // -------------------------
                var uploadedFiles = Request.Files;
                int order = package.PackagePhotos.Count + 1;

                for (int i = 0; i < uploadedFiles.Count; i++)
                {
                    var file = uploadedFiles[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var path = Server.MapPath("~/Uploads/PackagePhotos/" + fileName);
                        file.SaveAs(path);

                        package.PackagePhotos.Add(new PackagePhoto
                        {
                            PhotoUrl = "/Uploads/PackagePhotos/" + fileName,
                            DisplayOrder = order++
                        });
                    }
                }

                // SAVE CHANGES
                uow.PackageRepository.Update(package);
                uow.Save();
            }

            TempData["ToastrType"] = "success";
            TempData["ToastrMessage"] = "Package updated successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            try
            {
                var package = _packageService.GetById(id);
                if (package == null)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Package not found.";
                    return RedirectToAction("Index");
                }

                return View(package);
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Unable to load delete confirmation.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _packageService.Delete(id);
                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Package deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while deleting the package.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult ExportExcel()
        {
            try
            {
                var packages = _packageService.GetAll().ToList();

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Packages");

                    // Header row
                    string[] headers = { "ID", "Name", "Description", "Base Price", "Duration (Hrs)", "Status", "Soft Copies", "Edited Photos Count", "Items", "Event Types" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[1, i + 1].Value = headers[i];
                        ws.Cells[1, i + 1].Style.Font.Bold = true;
                        ws.Cells[1, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }

                    int row = 2;
                    foreach (var pkg in packages)
                    {
                        ws.Cells[row, 1].Value = pkg.PackageId;
                        ws.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[row, 2].Value = pkg.Name;
                        ws.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 3].Value = pkg.Description ?? "";
                        ws.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 4].Value = pkg.BasePrice;
                        ws.Cells[row, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 5].Value = pkg.DurationHours ?? 0;
                        ws.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[row, 6].Value = pkg.IsActive ? "Active" : "Inactive";
                        ws.Cells[row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[row, 7].Value = pkg.IsIncludeSodftCopies ? "Yes" : "No";
                        ws.Cells[row, 7].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[row, 8].Value = pkg.EditedPhotosCount ?? 0;
                        ws.Cells[row, 8].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[row, 9].Value = string.Join(", ", pkg.PackageItems.Select(i => i.Name));
                        ws.Cells[row, 10].Value = string.Join(", ", pkg.PackageEventTypes.Select(e => e.EventType.Name));

                        // Apply borders to each cell in the row
                        for (int col = 1; col <= 10; col++)
                        {
                            ws.Cells[row, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                        }

                        row++;
                    }

                    // Auto-fit columns
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"Packages_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while exporting Excel: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public ActionResult ExportPdf()
        {
            try
            {
                var packages = _packageService.GetAll().ToList();

                using (var stream = new MemoryStream())
                {
                    Document pdfDoc = new Document(PageSize.A4.Rotate(), 10f, 10f, 10f, 10f);
                    PdfWriter.GetInstance(pdfDoc, stream);

                    pdfDoc.Open();

                    // Define headers
                    string[] headers = { "ID", "Name", "Description", "Base Price", "Duration", "Status", "Soft Copies", "Edited Photos", "Items", "Event Types" };
                    var table = new PdfPTable(headers.Length) { WidthPercentage = 100 };
                    table.SetWidths(new float[] { 5f, 15f, 20f, 10f, 7f, 7f, 10f, 7f, 15f, 15f });

                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                    Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                    // Add header cells
                    foreach (var h in headers)
                    {
                        table.AddCell(new PdfPCell(new Phrase(h, headerFont)));
                    }

                    // Add data rows
                    foreach (var pkg in packages)
                    {
                        table.AddCell(new PdfPCell(new Phrase(pkg.PackageId.ToString(), bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                        table.AddCell(new PdfPCell(new Phrase(pkg.Name, bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_RIGHT
                        });
                        table.AddCell(new PdfPCell(new Phrase(pkg.Description ?? "", bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_RIGHT
                        });
                        table.AddCell(new PdfPCell(new Phrase(pkg.BasePrice.ToString("N2"), bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_RIGHT
                        });
                        table.AddCell(new PdfPCell(new Phrase((pkg.DurationHours ?? 0).ToString(), bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                        table.AddCell(new PdfPCell(new Phrase(pkg.IsActive ? "Active" : "Inactive", bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                        table.AddCell(new PdfPCell(new Phrase(pkg.IsIncludeSodftCopies ? "Yes" : "No", bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                        table.AddCell(new PdfPCell(new Phrase((pkg.EditedPhotosCount ?? 0).ToString(), bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                        table.AddCell(new PdfPCell(new Phrase(string.Join(", ", pkg.PackageItems.Select(i => i.Name)), bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_LEFT
                        });
                        table.AddCell(new PdfPCell(new Phrase(string.Join(", ", pkg.PackageEventTypes.Select(e => e.EventType.Name)), bodyFont))
                        {
                            HorizontalAlignment = Element.ALIGN_LEFT
                        });
                    }

                    pdfDoc.Add(table);
                    pdfDoc.Close();

                    string fileName = $"Packages_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                    return File(stream.ToArray(), "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while exporting PDF: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public ActionResult ViewSummaryImage(int id)
        {
            try
            {
                var package = _packageService.GetById(id);
                if (package == null)
                    return HttpNotFound("Package not found.");

                var image = GeneratePackageSummaryImage(package);

                if (image == null || image.Length == 0)
                    return new HttpStatusCodeResult(500, "Failed to generate summary image.");

                return File(image, "image/png");
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while generating the summary image.";
                return new HttpStatusCodeResult(500, "Error generating summary image: " + ex.Message);
            }
        }

        public ActionResult DownloadSummaryImage(int id)
        {
            try
            {
                var package = _packageService.GetById(id);
                if (package == null)
                    return HttpNotFound("Package not found.");

                var image = GeneratePackageSummaryImage(package);

                if (image == null || image.Length == 0)
                    return new HttpStatusCodeResult(500, "Failed to generate summary image.");

                string fileName = $"Package_{id}_Summary.png";

                return File(image, "image/png", fileName);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "An error occurred while downloading the summary image.";
                return new HttpStatusCodeResult(500, "Error downloading summary image: " + ex.Message);
            }
        }

        public ActionResult DownloadSummaryPdf(int id)
        {
            try
            {
                var package = _packageService.GetById(id);
                if (package == null)
                    return HttpNotFound("Package not found.");

                // Generate the summary image
                var imageBytes = GeneratePackageSummaryImage(package);
                if (imageBytes == null || imageBytes.Length == 0)
                    return new HttpStatusCodeResult(500, "Failed to generate summary image.");

                using (var ms = new MemoryStream())
                {
                    // Create the PDF
                    Document pdf = new Document(PageSize.A4, 20, 20, 20, 20);
                    PdfWriter writer = PdfWriter.GetInstance(pdf, ms);

                    pdf.Open();

                    // Convert image bytes to iTextSharp Image
                    iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(imageBytes);

                    // Scale to fit A4 width
                    img.ScaleToFit(pdf.PageSize.Width - 40, pdf.PageSize.Height - 40);
                    img.Alignment = Element.ALIGN_CENTER;

                    pdf.Add(img);
                    pdf.Close();

                    string fileName = $"Package_{id}_Summary.pdf";
                    return File(ms.ToArray(), "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Error generating summary PDF.";
                return new HttpStatusCodeResult(500, "Error generating summary PDF: " + ex.Message);
            }
        }

        public ActionResult DownloadAllSummariesPdf()
        {
            try
            {
                var packages = _packageService.GetAll().ToList();
                if (!packages.Any())
                    return new HttpStatusCodeResult(404, "No packages available.");

                using (var ms = new MemoryStream())
                {
                    Document pdf = new Document(PageSize.A4, 20, 20, 20, 20);
                    PdfWriter writer = PdfWriter.GetInstance(pdf, ms);

                    pdf.Open();

                    foreach (var package in packages)
                    {
                        // Generate summary image for each package
                        var imageBytes = GeneratePackageSummaryImage(package);

                        if (imageBytes != null && imageBytes.Length > 0)
                        {
                            iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(imageBytes);

                            img.ScaleToFit(pdf.PageSize.Width - 40, pdf.PageSize.Height - 40);
                            img.Alignment = Element.ALIGN_CENTER;

                            // Add image to the PDF
                            pdf.Add(img);
                        }

                        // Add a new page AFTER each package (except last one)
                        if (package != packages.Last())
                            pdf.NewPage();
                    }

                    pdf.Close();

                    string fileName = $"All_Packages_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                    return File(ms.ToArray(), "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error generating combined PDF: " + ex.Message);
            }
        }

        private byte[] GeneratePackageSummaryImage(Package package)
        {
            int width = 1500;
            int height = 1100;

            var info = new SKImageInfo(width, height);

            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                // ============================
                // FONTS
                // ============================
                var fontBold = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
                var fontRegular = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);

                // Helper to draw text
                Action<string, float, float, float, SKTypeface, SKColor, SKTextAlign> DrawText =
                    (text, x, y, size, font, color, align) =>
                    {
                        using (var paint = new SKPaint())
                        {
                            paint.Typeface = font;
                            paint.TextSize = size;
                            paint.Color = color;
                            paint.IsAntialias = true;
                            paint.TextAlign = align;
                            canvas.DrawText(text, x, y, paint);
                        }
                    };

                // ============================
                // LEFT IMAGE COLUMN
                // ============================
                int colX = 50;
                int colY = 50;
                int colW = 450;
                int colH = 1000;

                var photos = package.PackagePhotos
                    .OrderBy(x => x.DisplayOrder)
                    .Take(4)
                    .ToList();

                if (photos.Count == 1)
                {
                    try
                    {
                        string abs = Url.Content(photos[0].PhotoUrl).Replace("~", "");
                        string fullUrl = Request.Url.GetLeftPart(UriPartial.Authority) + abs;

                        using (var wc = new WebClient())
                        {
                            var bytes = wc.DownloadData(fullUrl);
                            using (var skimg = SKImage.FromEncodedData(bytes))
                            {
                                canvas.DrawImage(skimg, SKRect.Create(colX, colY, colW, colH));
                            }
                        }
                    }
                    catch { }
                }
                else if (photos.Count > 1)
                {
                    int spacing = 15;
                    int eachH = (colH - (spacing * (photos.Count - 1))) / photos.Count;
                    int drawY = colY;

                    foreach (var p in photos)
                    {
                        try
                        {
                            string abs = Url.Content(p.PhotoUrl).Replace("~", "");
                            string fullUrl = Request.Url.GetLeftPart(UriPartial.Authority) + abs;

                            using (var wc = new WebClient())
                            {
                                var bytes = wc.DownloadData(fullUrl);
                                using (var skimg = SKImage.FromEncodedData(bytes))
                                {
                                    canvas.DrawImage(skimg, SKRect.Create(colX, drawY, colW, eachH));
                                }
                            }
                        }
                        catch { }

                        drawY += eachH + spacing;
                    }
                }

                // ============================
                // RIGHT SIDE CONTENT
                // ============================
                float centerX = 1050;

                // PACKAGE TITLE
                DrawText(package.Name.ToUpper(), centerX, 200, 80, fontBold, SKColors.Black, SKTextAlign.Center);

                // ==================================================
                // DESCRIPTION
                // ==================================================
                float textY = 270;
                float lineGap = 45;
                float sectionGap = lineGap * 2;

                if (!string.IsNullOrWhiteSpace(package.Description))
                {
                    var descParts = package.Description
                        .Split(new[] { ',', '/', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim());

                    foreach (var part in descParts)
                    {
                        if (part.Contains("&"))
                        {
                            var split = part.Split('&').Select(s => s.Trim()).ToArray();

                            // First line (before "&")
                            DrawText(split[0], centerX, textY, 44, fontBold, SKColors.Black, SKTextAlign.Center);
                            textY += lineGap;

                            // Second line (after "&")
                            DrawText("& " + split[1], centerX, textY, 38, fontBold, SKColors.Black, SKTextAlign.Center);
                            textY += lineGap;
                        }
                        else
                        {
                            DrawText(part, centerX, textY, 38, fontBold, SKColors.Black, SKTextAlign.Center);
                            textY += lineGap;
                        }
                    }
                }

                // Space before EVENT TYPES
                textY += sectionGap;

                // ==================================================
                // EVENT TYPES
                // ==================================================
                foreach (var evt in package.PackageEventTypes.Select(e => e.EventType.Name))
                {
                    DrawText(evt.ToUpper(), centerX, textY, 38, fontBold, SKColors.Black, SKTextAlign.Center);
                    textY += lineGap;
                }

                // Space before ITEMS
                textY += sectionGap;

                // ==================================================
                // ITEMS
                // ==================================================
                List<string> items = new List<string>();

                foreach (var item in package.PackageItems.OrderBy(i => i.PackageItemId))
                    items.Add(item.Name);

                if (package.IsIncludeSodftCopies)
                    items.Add("All soft copies");

                if (package.EditedPhotosCount.HasValue)
                    items.Add($"{package.EditedPhotosCount} Edited Photos");

                foreach (var item in items)
                {
                    DrawText(item, centerX, textY, 38, fontRegular, SKColors.Black, SKTextAlign.Center);
                    textY += lineGap;
                }

                // ============================
                // PRICE
                // ============================
                DrawText(
                    $"{package.BasePrice:N0} LKR",
                    centerX,
                    1030,
                    55,
                    fontBold,
                    SKColors.Black,
                    SKTextAlign.Center
                );

                // ============================
                // EXPORT
                // ============================
                using (var img = surface.Snapshot())
                using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        [HttpPost]
        public async Task<ActionResult> SendPackageEmail(SendPackageEmailViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.CustomerEmail))
                {
                    TempData["ToastrType"] = "error";
                    TempData["ToastrMessage"] = "Customer email is required.";
                    return RedirectToAction("SendPackageEmail", new { packageId = model.PackageId });
                }

                // Get package
                var package = _packageService.GetById(model.PackageId);

                // Generate PDF
                byte[] pdfBytes = GenerateSinglePackagePdf(package);

                // Send with SendGrid
                await SendEmailWithAttachment_SendGrid(
                    model.CustomerEmail,
                    model.Subject,
                    model.Message,
                    pdfBytes,
                    $"Package_{package.PackageId}.pdf"
                );

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Email sent successfully!";
                return RedirectToAction("Details", new { id = model.PackageId });
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to send email: " + ex.Message;
                return RedirectToAction("SendPackageEmail", new { packageId = model.PackageId });
            }
        }

        private byte[] GenerateSinglePackagePdf(Package pkg)
        {
            using (var ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4.Rotate());
                PdfWriter.GetInstance(pdfDoc, ms);

                pdfDoc.Open();

                PdfPTable table = new PdfPTable(10) { WidthPercentage = 100 };
                string[] headers = { "ID", "Name", "Description", "Base Price", "Duration", "Status", "Soft Copies", "Edited Photos", "Items", "Event Types" };

                foreach (var h in headers)
                    table.AddCell(new Phrase(h));

                table.AddCell(pkg.PackageId.ToString());
                table.AddCell(pkg.Name);
                table.AddCell(pkg.Description ?? "");
                table.AddCell(pkg.BasePrice.ToString("N2"));
                table.AddCell((pkg.DurationHours ?? 0).ToString());
                table.AddCell(pkg.IsActive ? "Active" : "Inactive");
                table.AddCell(pkg.IsIncludeSodftCopies ? "Yes" : "No");
                table.AddCell(pkg.EditedPhotosCount?.ToString() ?? "0");
                table.AddCell(string.Join(", ", pkg.PackageItems.Select(i => i.Name)));
                table.AddCell(string.Join(", ", pkg.PackageEventTypes.Select(e => e.EventType.Name)));

                pdfDoc.Add(table);
                pdfDoc.Close();

                return ms.ToArray();
            }
        }

        private async Task SendEmailWithAttachment_SendGrid(string toEmail, string subject, string htmlMessage, byte[] attachmentBytes, string attachmentName)
        {
            string apiKey = ConfigurationManager.AppSettings["SendGridKey"];
            string senderEmail = ConfigurationManager.AppSettings["SenderEmail"];
            string senderName = ConfigurationManager.AppSettings["SenderName"];

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(senderEmail, senderName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlMessage);

            // Add attachment
            if (attachmentBytes != null)
            {
                string base64File = Convert.ToBase64String(attachmentBytes);

                msg.AddAttachment(new Attachment
                {
                    Content = base64File,
                    Filename = attachmentName,
                    Type = "application/pdf",
                    Disposition = "attachment"
                });
            }

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                throw new Exception("SendGrid send failed: " + response.StatusCode);
            }
        }
    }
}