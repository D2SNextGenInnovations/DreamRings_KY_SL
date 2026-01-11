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

namespace MrGroom_KY_SL.Business.Services
{
    public class PackagePdfService
    {
        private readonly PackageService _packageService = new PackageService();
        private readonly CompanyInfoService _companyInfoService = new CompanyInfoService();

        public byte[] GenerateSinglePackagePdf(Package pkg)
        {
            using (var ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 30f, 30f, 20f, 20f);
                PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                // Fonts
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20);
                Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                Font bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                Font priceFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22);
                Font nameFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                Font detailFont = FontFactory.GetFont(FontFactory.HELVETICA, 9); // slightly smaller font

                // ===============================================================
                // COMPANY HEADER (Logo left, details right aligned)
                // ===============================================================
                var companyInfoService = new CompanyInfoService();
                var company = companyInfoService.GetAll().FirstOrDefault();

                if (company != null)
                {
                    PdfPTable headerTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100
                    };

                    headerTable.SetWidths(new float[] { 20f, 5f, 75f });

                    // ========================= LEFT: LOGO =========================
                    if (company.CompanyLogo != null && company.CompanyLogo.Length > 0)
                    {
                        Image logo = Image.GetInstance(company.CompanyLogo);
                        logo.ScaleAbsolute(100, 100); // Bigger logo

                        PdfPCell logoCell = new PdfPCell(logo)
                        {
                            Border = Rectangle.NO_BORDER,
                            Rowspan = 4,
                            VerticalAlignment = Element.ALIGN_TOP,
                            PaddingTop = -15, // Move logo slightly up
                            PaddingRight = 5
                        };

                        headerTable.AddCell(logoCell);
                    }
                    else
                    {
                        headerTable.AddCell(new PdfPCell(new Phrase(""))
                        {
                            Border = Rectangle.NO_BORDER,
                            Rowspan = 4
                        });
                    }

                    // ========================= MIDDLE SPACER ======================
                    headerTable.AddCell(new PdfPCell(new Phrase(""))
                    {
                        Border = Rectangle.NO_BORDER,
                        Rowspan = 4
                    });

                    // ========================= RIGHT: DETAILS (RIGHT ALIGNED) ======================
                    PdfPCell nameCell = new PdfPCell(new Phrase(company.CompanyName, nameFont))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        PaddingBottom = 4
                    };
                    headerTable.AddCell(nameCell);

                    headerTable.AddCell(new PdfPCell(new Phrase($"Address: {company.Address}", detailFont))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        PaddingBottom = 4
                    });

                    string phoneText = $"Phone: {company.Phone}";
                    if (!string.IsNullOrWhiteSpace(company.SecondaryPhone))
                        phoneText += $" / {company.SecondaryPhone}";

                    headerTable.AddCell(new PdfPCell(new Phrase(phoneText, detailFont))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        PaddingBottom = 4
                    });

                    headerTable.AddCell(new PdfPCell(new Phrase($"Email: {company.Email}", detailFont))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        PaddingBottom = 4
                    });

                    pdfDoc.Add(headerTable);
                    pdfDoc.Add(new Paragraph("\n")); // space under header
                }

                // ===============================================================
                // BEGIN PACKAGE CONTENT
                // ===============================================================

                pdfDoc.Add(new Paragraph("\n"));

                // PACKAGE NAME
                Paragraph namePara = new Paragraph(pkg.Name, titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                };
                pdfDoc.Add(namePara);

                // DESCRIPTION
                if (!string.IsNullOrWhiteSpace(pkg.Description))
                {
                    Paragraph descPara = new Paragraph(pkg.Description, subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 15f
                    };
                    pdfDoc.Add(descPara);
                }

                // EVENT TYPES
                foreach (var ev in pkg.PackageEventTypes)
                {
                    Paragraph evPara = new Paragraph(ev.EventType?.Name ?? "", bodyFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f
                    };
                    pdfDoc.Add(evPara);
                }

                pdfDoc.Add(new Paragraph("\n"));

                // ITEMS
                foreach (var item in pkg.PackageItems.OrderBy(i => i.Name))
                {
                    Paragraph itemPara = new Paragraph(item.Name, bodyFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f
                    };
                    pdfDoc.Add(itemPara);
                }

                // PRICE
                pdfDoc.Add(new Paragraph("\n"));
                Paragraph pricePara = new Paragraph($"{pkg.BasePrice:N2} LKR", priceFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                pdfDoc.Add(pricePara);

                pdfDoc.Close();
                return ms.ToArray();
            }
        }
    }
}
