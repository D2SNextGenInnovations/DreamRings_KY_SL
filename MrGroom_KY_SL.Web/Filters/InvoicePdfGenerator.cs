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
            var companyInfo = new CompanyInfoService().GetAll().FirstOrDefault();

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 25, 25, 25, 30);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                string fontPath = HttpContext.Current.Server.MapPath("~/Content/Fonts/seguisym.ttf");
                BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                Font normal = new Font(bf, 9);
                Font bold = new Font(bf, 9, Font.BOLD);
                Font title = new Font(bf, 12, Font.BOLD);
                Font footerBold = new Font(bf, 11, Font.BOLD);

                BaseColor grey = new BaseColor(235, 235, 235);

                // ---------------- HEADER ----------------
                PdfPTable header = new PdfPTable(2);
                header.WidthPercentage = 100;
                header.SetWidths(new float[] { 60, 40 });

                PdfPTable left = new PdfPTable(1);
                if (companyInfo?.CompanyLogo != null)
                {
                    Image logo = Image.GetInstance(companyInfo.CompanyLogo);
                    logo.ScaleToFit(180, 60);
                    left.AddCell(NoBorderCell(logo));
                }

                StringBuilder ci = new StringBuilder();
                if (!string.IsNullOrEmpty(companyInfo?.Phone)) ci.AppendLine("☎ " + companyInfo.Phone);
                if (!string.IsNullOrEmpty(companyInfo?.Address)) ci.AppendLine("📍 " + companyInfo.Address);
                if (!string.IsNullOrEmpty(companyInfo?.Email)) ci.AppendLine("✉ " + companyInfo.Email);

                left.AddCell(NoBorderCell(new Phrase(ci.ToString(), normal)));
                header.AddCell(NoBorderCell(left));

                PdfPTable right = new PdfPTable(1);
                right.AddCell(Cell("INVOICE", title, Element.ALIGN_CENTER, grey));
                right.AddCell(Cell("Invoice No : INV-" + b.BookingId.ToString("D6"), normal));
                right.AddCell(Cell("Date : " + DateTime.Now.ToString("yyyy-MM-dd"), normal));
                right.AddCell(Cell("Client : " + b.Customer.FirstName + " " + b.Customer.LastName, normal));
                right.AddCell(Cell("Phone : " + b.Customer.Phone, normal));
                header.AddCell(NoBorderCell(right));

                doc.Add(header);
                doc.Add(new Paragraph("\n"));

                // ---------------- EVENT DETAILS ----------------
                PdfPTable details = new PdfPTable(2);
                details.WidthPercentage = 100;
                details.AddCell(Cell("Event Date : " + b.EventDate.ToString("yyyy-MM-dd"), normal));
                details.AddCell(Cell("Venue : " + b.Location, normal));
                doc.Add(details);
                doc.Add(new Paragraph("\n"));

                // ---------------- PACKAGE CONTENTS ----------------
                PdfPTable pkg = new PdfPTable(4);
                pkg.WidthPercentage = 100;
                pkg.SetWidths(new float[] { 50, 15, 15, 20 });

                string packageName = b.Package != null ? b.Package.Name : "Package";

                pkg.AddCell(Cell($"Package Contents ({packageName})", bold, bg: grey, colspan: 4));
                pkg.AddCell(Cell("Description", bold, bg: grey));
                pkg.AddCell(Cell("Qty", bold, Element.ALIGN_CENTER, grey));
                pkg.AddCell(Cell("Price", bold, Element.ALIGN_RIGHT, grey));
                pkg.AddCell(Cell("Total", bold, Element.ALIGN_RIGHT, grey));

                decimal packageTotal = 0m;

                // Package Items
                foreach (var item in b.Package.PackageItemPackages)
                {
                    decimal total = item.CalculatedPrice;
                    packageTotal += total;

                    pkg.AddCell(Cell(item.PackageItem.Name, normal));
                    pkg.AddCell(Cell(item.Qty.ToString(), normal, Element.ALIGN_CENTER));
                    pkg.AddCell(Cell(Money(item.UnitPrice), normal, Element.ALIGN_RIGHT));
                    pkg.AddCell(Cell(Money(total), normal, Element.ALIGN_RIGHT));
                }

                // Package Event Types
                foreach (var ev in b.Package.PackageEventTypes)
                {
                    decimal total = ev.UnitPrice;
                    packageTotal += total;

                    pkg.AddCell(Cell(ev.EventType.Name, normal));
                    pkg.AddCell(Cell("1", normal, Element.ALIGN_CENTER));
                    pkg.AddCell(Cell(Money(ev.UnitPrice), normal, Element.ALIGN_RIGHT));
                    pkg.AddCell(Cell(Money(total), normal, Element.ALIGN_RIGHT));
                }

                pkg.AddCell(Cell("Package Total", bold, Element.ALIGN_RIGHT, colspan: 3));
                pkg.AddCell(Cell(Money(packageTotal), bold, Element.ALIGN_RIGHT));
                doc.Add(pkg);

                doc.Add(new Paragraph("\n"));


                // ---------------- BOOKING SUMMARY (EVENT TYPES + ADDONS) ----------------
                PdfPTable addons = new PdfPTable(4);
                addons.WidthPercentage = 100;
                addons.SetWidths(new float[] { 50, 15, 15, 20 });

                addons.AddCell(Cell("Booking Summary (Add-ons)", bold, bg: grey, colspan: 4));
                addons.AddCell(Cell("Description", bold, bg: grey));
                addons.AddCell(Cell("Qty", bold, Element.ALIGN_CENTER, grey));
                addons.AddCell(Cell("Price", bold, Element.ALIGN_RIGHT, grey));
                addons.AddCell(Cell("Total", bold, Element.ALIGN_RIGHT, grey));

                decimal addonsTotal = 0m;

                //Selected Event Types (EXTRA / BOOKED)
                if (b.BookingEventTypes != null)
                {
                    var groupedEvents = b.BookingEventTypes
                        .GroupBy(x => x.EventType);

                    foreach (var g in groupedEvents)
                    {
                        int qty = g.Count();
                        decimal price = g.Key.Price;
                        decimal total = qty * price;
                        addonsTotal += total;

                        addons.AddCell(Cell(g.Key.Name, normal));
                        addons.AddCell(Cell(qty.ToString(), normal, Element.ALIGN_CENTER));
                        addons.AddCell(Cell(Money(price), normal, Element.ALIGN_RIGHT));
                        addons.AddCell(Cell(Money(total), normal, Element.ALIGN_RIGHT));
                    }
                }

                // Add-ons
                if (b.BookingAddons != null)
                {
                    foreach (var a in b.BookingAddons.Where(x => x.Quantity > 0))
                    {
                        decimal total = a.Quantity * a.UnitPrice;
                        addonsTotal += total;

                        addons.AddCell(Cell(a.PackageItem.Name, normal));
                        addons.AddCell(Cell(a.Quantity.ToString(), normal, Element.ALIGN_CENTER));
                        addons.AddCell(Cell(Money(a.UnitPrice), normal, Element.ALIGN_RIGHT));
                        addons.AddCell(Cell(Money(total), normal, Element.ALIGN_RIGHT));
                    }
                }

                addons.AddCell(Cell("Add-ons Total", bold, Element.ALIGN_RIGHT, colspan: 3));
                addons.AddCell(Cell(Money(addonsTotal), bold, Element.ALIGN_RIGHT));
                doc.Add(addons);

                doc.Add(new Paragraph("\n"));

                // ---------------- GRAND TOTAL ----------------
                PdfPTable grand = new PdfPTable(2);
                grand.WidthPercentage = 40;
                grand.HorizontalAlignment = Element.ALIGN_RIGHT;

                decimal grandTotal = packageTotal + addonsTotal;

                grand.AddCell(Cell("Grand Total", bold, bg: grey));
                grand.AddCell(Cell(Money(grandTotal), bold, Element.ALIGN_RIGHT, grey));
                doc.Add(grand);
                doc.Add(new Paragraph("\n"));

                // ---------------- PAYMENT DETAILS ----------------
                if (b.Payments != null && b.Payments.Any())
                {
                    PdfPTable pay = new PdfPTable(2);
                    pay.WidthPercentage = 100;
                    pay.SetWidths(new float[] { 70, 30 });

                    pay.AddCell(Cell("Payment Details", bold, bg: grey, colspan: 2));

                    decimal paidTotal = 0m;

                    foreach (var p in b.Payments.OrderBy(x => x.PaymentDate))
                    {
                        paidTotal += p.Amount;

                        string leftText =
                            $"{p.PaymentType}" +
                            (p.PaymentDate != DateTime.MinValue
                                ? $" ({p.PaymentDate:yyyy-MM-dd})"
                                : "");

                        pay.AddCell(Cell(leftText, normal));
                        pay.AddCell(Cell(Money(p.Amount), normal, Element.ALIGN_RIGHT));
                    }

                    decimal balance = grandTotal - paidTotal;

                    PdfPTable balanceTbl = new PdfPTable(2);
                    balanceTbl.WidthPercentage = 40;
                    balanceTbl.HorizontalAlignment = Element.ALIGN_RIGHT;

                    balanceTbl.AddCell(Cell("Balance Due", bold, bg: grey));
                    balanceTbl.AddCell(Cell(Money(balance), bold, Element.ALIGN_RIGHT, grey));

                    doc.Add(balanceTbl);

                    // ---- TOTAL PAID ----
                    pay.AddCell(Cell("Total Paid", bold, Element.ALIGN_RIGHT));
                    pay.AddCell(Cell(Money(paidTotal), bold, Element.ALIGN_RIGHT));

                    doc.Add(pay);
                    doc.Add(new Paragraph("\n"));
                }


                // ---------------- SIGNATURE ----------------
                PdfPTable sign = new PdfPTable(2);
                sign.WidthPercentage = 100;
                sign.SetWidths(new float[] { 60, 40 });

                sign.AddCell(NoBorderCell("Customer Signature:\n\n______________________________"));
                sign.AddCell(NoBorderCell(""));
                doc.Add(sign);

                doc.Add(new Paragraph("\n"));

                // ---------------- FOOTER ----------------
                Paragraph footer = new Paragraph(
@"*** Terms and conditions ***
----------------------------
Albums & other prices may vary according to the Dollar rate.
Full amount should be paid one week prior to your function.
You have to reserve your date by paying an advance of Rs.10,000.00 (Non refundable)",
                    normal);

                doc.Add(footer);
                doc.Add(new Paragraph("\n"));

                Paragraph thanks = new Paragraph("Thank You Business with Dream Rings",
                    footerBold);
                thanks.Alignment = Element.ALIGN_CENTER;
                doc.Add(thanks);

                doc.Close();
                return ms.ToArray();
            }
        }

        private static string Money(decimal v)
        {
            return "Rs. " + v.ToString("N2");
        }

        private static PdfPCell Cell(string text, Font font,
            int align = Element.ALIGN_LEFT, BaseColor bg = null, int colspan = 1)
        {
            PdfPCell c = new PdfPCell(new Phrase(text, font));
            c.HorizontalAlignment = align;
            c.Colspan = colspan;
            c.Padding = 5;
            if (bg != null) c.BackgroundColor = bg;
            return c;
        }

        private static PdfPCell NoBorderCell(IElement element)
        {
            PdfPCell c = new PdfPCell();
            c.AddElement(element);
            c.Border = Rectangle.NO_BORDER;
            return c;
        }

        private static PdfPCell NoBorderCell(string text)
        {
            PdfPCell c = new PdfPCell(new Phrase(text));
            c.Border = Rectangle.NO_BORDER;
            return c;
        }
    }

    //private static PdfPCell Cell(string text, Font font, int align = Element.ALIGN_LEFT,
    //                                 BaseColor bg = null, int colspan = 1)
    //    {
    //        PdfPCell c = new PdfPCell(new Phrase(text, font));
    //        c.HorizontalAlignment = align;
    //        c.Colspan = colspan;
    //        c.Padding = 6;
    //        if (bg != null) c.BackgroundColor = bg;
    //        return c;
    //    }
    //}
}
