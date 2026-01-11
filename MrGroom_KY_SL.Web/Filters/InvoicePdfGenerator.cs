using iTextSharp.text;
using iTextSharp.text.pdf;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MrGroom_KY_SL.Business.Services;

namespace MrGroom_KY_SL.Models
{
    public class InvoicePdfGenerator
    {
        public static byte[] GenerateInvoice(Booking b)
        {
            // Load company info (assuming only 1 row)

           var companyInfo = new CompanyInfoService().GetAll().FirstOrDefault();

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 25, 25, 25, 25);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();
                decimal grandTotal = 0;

                string fontPath = HttpContext.Current.Server.MapPath("~/Content/Fonts/seguisym.ttf");
                BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                //Font bold14 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                //Font bold12 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                //Font normal11 = FontFactory.GetFont(FontFactory.HELVETICA, 11);

                Font normal11 = new Font(bf, 11, Font.NORMAL);
                Font bold12 = new Font(bf, 12, Font.BOLD);
                Font bold14 = new Font(bf, 14, Font.BOLD);

                BaseColor grey = new BaseColor(230, 230, 230);

                // ---------------- HEADER ----------------
                PdfPTable header = new PdfPTable(2);
                header.WidthPercentage = 100;
                header.SetWidths(new float[] { 60, 40 });

                // LEFT SIDE (logo + company info)
                PdfPTable leftBlock = new PdfPTable(1);
                leftBlock.WidthPercentage = 100;

                //string logoPath = HttpContext.Current.Server.MapPath("~/Content/Images/newReportLogo.png");
                //if (File.Exists(logoPath))
                //{
                //    Image logo = Image.GetInstance(logoPath);
                //    logo.ScaleAbsolute(240, 54);
                //    logo.Alignment = Image.ALIGN_LEFT;
                //    PdfPCell logoCell = new PdfPCell(logo);
                //    logoCell.Border = Rectangle.NO_BORDER;
                //    header.AddCell(logoCell);
                //}
                //else
                //{
                //    header.AddCell(new PdfPCell(new Phrase("Dream Rings Photography", bold14)) { Border = Rectangle.NO_BORDER });
                //}
                // ---------- LOGO ----------
                if (companyInfo?.CompanyLogo != null && companyInfo.CompanyLogo.Length > 0)
                {
                    Image logo = Image.GetInstance(companyInfo.CompanyLogo);
                    logo.ScaleAbsolute(200, 60);
                    logo.Alignment = Image.ALIGN_LEFT;

                    PdfPCell logoCell = new PdfPCell(logo);
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.PaddingBottom = 5;
                    leftBlock.AddCell(logoCell);
                }
                else
                {
                    PdfPCell noLogo = new PdfPCell(new Phrase(companyInfo?.CompanyName ?? "Company Name", bold14));
                    noLogo.Border = Rectangle.NO_BORDER;
                    leftBlock.AddCell(noLogo);
                }

                // ---------- COMPANY INFO ----------
                StringBuilder ci = new StringBuilder();

                string phoneCombined = null;

                // Combine phone numbers
                if (!string.IsNullOrEmpty(companyInfo?.Phone) &&
                    !string.IsNullOrEmpty(companyInfo?.SecondaryPhone))
                {
                    phoneCombined = $"{companyInfo.Phone} / {companyInfo.SecondaryPhone}";
                }
                else if (!string.IsNullOrEmpty(companyInfo?.Phone))
                {
                    phoneCombined = companyInfo.Phone;
                }
                else if (!string.IsNullOrEmpty(companyInfo?.SecondaryPhone))
                {
                    phoneCombined = companyInfo.SecondaryPhone;
                }

                // Phone with icon 📞
                if (!string.IsNullOrEmpty(phoneCombined))
                    ci.AppendLine("\u260E  " + phoneCombined);

                // Address with icon 📍
                if (!string.IsNullOrEmpty(companyInfo?.Address))
                    ci.AppendLine("\uD83D\uDCCC  " + companyInfo.Address);

                // Email with icon ✉️
                if (!string.IsNullOrEmpty(companyInfo?.Email))
                    ci.AppendLine("\u2709  " + companyInfo.Email);

                PdfPCell infoCell = new PdfPCell(new Phrase(ci.ToString(), normal11));
                infoCell.Border = Rectangle.NO_BORDER;
                infoCell.PaddingTop = 5;
                leftBlock.AddCell(infoCell);

                // Add left block to header table
                PdfPCell finalLeft = new PdfPCell(leftBlock);
                finalLeft.Border = Rectangle.NO_BORDER;
                header.AddCell(finalLeft);

                PdfPTable box = new PdfPTable(1);
                box.AddCell(Cell("INVOICE", bold14, Element.ALIGN_CENTER, grey));
                box.AddCell(Cell("Invoice #: " + b.BookingId, normal11));
                box.AddCell(Cell("Date: " + DateTime.Now.ToString("yyyy-MM-dd"), normal11));
                box.AddCell(Cell("Client: " + b.Customer.FirstName + " " + b.Customer.LastName, normal11));
                box.AddCell(Cell("Phone: " + b.Customer.Phone, normal11));

                PdfPCell boxCell = new PdfPCell(box);
                boxCell.Border = Rectangle.NO_BORDER;
                header.AddCell(boxCell);

                doc.Add(header);
                doc.Add(new Paragraph("\n"));

                // ---------------- DETAILS ----------------
                PdfPTable t1 = new PdfPTable(2);
                t1.WidthPercentage = 100;

                t1.AddCell(Cell("Event Type: " + b.EventType.Name, normal11));
                t1.AddCell(Cell("Event Date: " + b.EventDate.ToString("yyyy-MM-dd"), normal11));
                t1.AddCell(Cell("Venue: " + b.Location, normal11, colspan: 2));

                doc.Add(t1);
                doc.Add(new Paragraph("\n"));

                // ---------------- PACKAGE ITEMS TABLE ----------------
                PdfPTable pkg = new PdfPTable(4);
                pkg.WidthPercentage = 100;
                pkg.SetWidths(new float[] { 50, 15, 15, 20 });

                // Header
                pkg.AddCell(Cell("Package Items", bold12, bg: grey, colspan: 4));
                pkg.AddCell(Cell("Description", bold12, bg: grey));
                pkg.AddCell(Cell("Qty", bold12, Element.ALIGN_CENTER, grey));
                pkg.AddCell(Cell("Price", bold12, Element.ALIGN_RIGHT, grey));
                pkg.AddCell(Cell("Total", bold12, Element.ALIGN_RIGHT, grey));

                // Loop – Package Items
                decimal packageItemsTotal = 0m; // accumulate total
                var package = b.Package;

                if (package?.PackageItemPackages != null && package.PackageItemPackages.Any())
                {
                    foreach (var item in package.PackageItemPackages)
                    {
                        var itemName = item.PackageItem?.Name ?? "";
                        var qty = item.Qty;
                        var unitPrice = item.UnitPrice;
                        var total = item.CalculatedPrice;

                        packageItemsTotal += total;
                        grandTotal += total;

                        pkg.AddCell(Cell(itemName, normal11));
                        pkg.AddCell(Cell(qty.ToString(), normal11, Element.ALIGN_CENTER));
                        pkg.AddCell(Cell(unitPrice.ToString("N2"), normal11, Element.ALIGN_RIGHT));
                        pkg.AddCell(Cell(total.ToString("N2"), normal11, Element.ALIGN_RIGHT));
                    }
                }
                else
                {
                    pkg.AddCell(Cell("No items found", normal11, colspan: 4));
                }

                // Package Total
                pkg.AddCell(Cell("Package Total", bold12, Element.ALIGN_RIGHT, colspan: 3));
                pkg.AddCell(Cell(packageItemsTotal.ToString("N2"), bold12, Element.ALIGN_RIGHT));

                doc.Add(pkg);
                doc.Add(new Paragraph("\n"));


                // ---------------- EVENT TYPES TABLE ----------------
                var eventTypes = b.Package?.PackageEventTypes;

                // 3 columns: Description, Price, Total
                PdfPTable evt = new PdfPTable(3);
                evt.WidthPercentage = 100;
                evt.SetWidths(new float[] { 60, 20, 20 });

                evt.AddCell(Cell("Event Types", bold12, bg: grey, colspan: 3));

                // Header row
                evt.AddCell(Cell("Description", bold12, bg: grey));
                evt.AddCell(Cell("Price", bold12, Element.ALIGN_RIGHT, bg: grey));
                evt.AddCell(Cell("Total", bold12, Element.ALIGN_RIGHT, bg: grey));

                decimal eventTypesTotal = 0m;

                if (eventTypes != null && eventTypes.Any())
                {
                    foreach (var ev in eventTypes)
                    {
                        var name = ev.EventType?.Name ?? "";
                        var unit = ev.UnitPrice;
                        decimal qty = 1; // Event type normally 1 per booking
                        var total = qty * unit;

                        eventTypesTotal += total;
                        grandTotal += total;

                        evt.AddCell(Cell(name, normal11));
                        evt.AddCell(Cell(unit.ToString("N2"), normal11, Element.ALIGN_RIGHT));
                        evt.AddCell(Cell(total.ToString("N2"), normal11, Element.ALIGN_RIGHT));
                    }
                }
                else
                {
                    evt.AddCell(Cell("No event types selected", normal11, colspan: 3));
                }

                // Event Types Total row
                evt.AddCell(Cell("Event Types Total", bold12, Element.ALIGN_RIGHT, colspan: 2));
                evt.AddCell(Cell(eventTypesTotal.ToString("N2"), bold12, Element.ALIGN_RIGHT));

                doc.Add(evt);
                doc.Add(new Paragraph("\n"));

                // Add Full Amount section here
                PdfPTable fullAmountTable = new PdfPTable(2);
                fullAmountTable.WidthPercentage = 100;
                fullAmountTable.SetWidths(new float[] { 80, 20 });

                fullAmountTable.AddCell(Cell("Full Amount", bold12, Element.ALIGN_LEFT, grey));
                fullAmountTable.AddCell(Cell((packageItemsTotal + eventTypesTotal).ToString("N2"), bold12, Element.ALIGN_RIGHT));

                doc.Add(fullAmountTable);
                doc.Add(new Paragraph("\n"));

                // ---------------- PAYMENT SECTION ----------------
                PdfPTable pay = new PdfPTable(2);
                pay.WidthPercentage = 100;
                pay.SetWidths(new float[] { 70, 30 });

                pay.AddCell(Cell("Payment Details", bold12, bg: grey, colspan: 2));

                foreach (var p in b.Payments.OrderBy(x => x.PaymentDate))
                {
                    pay.AddCell(Cell($"{p.PaymentType} ({p.PaymentDate:yyyy-MM-dd})", normal11));
                    pay.AddCell(Cell(p.Amount.ToString("N2"), normal11, Element.ALIGN_RIGHT));
                }

                decimal paid = b.Payments.Sum(x => x.Amount);
                decimal balance = b.Package.BasePrice - paid;

                pay.AddCell(Cell("Balance Due", bold12));
                pay.AddCell(Cell(balance.ToString("N2"), bold12, Element.ALIGN_RIGHT));

                doc.Add(pay);
                doc.Close();

                return ms.ToArray();
            }
        }

        private static PdfPCell Cell(string text, Font font, int align = Element.ALIGN_LEFT,
                                     BaseColor bg = null, int colspan = 1)
        {
            PdfPCell c = new PdfPCell(new Phrase(text, font));
            c.HorizontalAlignment = align;
            c.Colspan = colspan;
            c.Padding = 6;
            if (bg != null) c.BackgroundColor = bg;
            return c;
        }
    }
}
