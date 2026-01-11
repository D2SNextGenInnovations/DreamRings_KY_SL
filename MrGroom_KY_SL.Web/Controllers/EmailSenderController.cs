using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using MrGroom_KY_SL.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [NoCache]
    public class EmailSenderController : Controller
    {
        private readonly CustomerService _customerService = new CustomerService();
        private readonly PackageService _packageService = new PackageService();
        private readonly PackagePdfService _packagePdfService = new PackagePdfService();
        private readonly EmailService _emailService = new EmailService();

        // GET: EmailSender
        public ActionResult Index()
        {
            var vm = new EmailSenderViewModel
            {
                Customers = _customerService.GetAll().OrderBy(c => c.CustomerId).Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = $"{c.FirstName} ({c.Email})"
                }).ToList(),

                Packages = _packageService.GetAll().OrderBy(p => p.PackageId).Select(p => new SelectListItem
                {
                    Value = p.PackageId.ToString(),
                    Text = p.Name
                }).ToList(),

                SendMode = EmailSendMode.SinglePackage
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Send(EmailSenderViewModel vm)
        {
            if (vm == null)
                return RedirectToAction("Index");

            // Basic validation
            if (vm.SelectedCustomerIds == null || !vm.SelectedCustomerIds.Any())
            {
                TempData["ToastrType"] = "warning";
                TempData["ToastrMessage"] = "Please select at least one customer to send email.";
                return RedirectToAction("Index");
            }

            byte[] attachment = null;
            string attachmentName = null;

            try
            {
                if (vm.SendMode == EmailSendMode.SinglePackage)
                {
                    if (!vm.PackageId.HasValue)
                        throw new Exception("Please select a package.");

                    var pkg = _packageService.GetById(vm.PackageId.Value);
                    if (pkg == null)
                        throw new Exception("Selected package not found.");

                    attachment = _packagePdfService.GenerateSinglePackagePdf(pkg);
                    attachmentName = $"Package_{pkg.PackageId}.pdf";
                }
                //else // All packages
                //{
                //    attachment = _packagePdfService.GenerateAllPackagesPdf();
                //    attachmentName = $"All_Packages_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                //}

                var sendResults = new List<EmailSendResult>();

                foreach (var custId in vm.SelectedCustomerIds)
                {
                    var cust = _customerService.GetById(custId);
                    if (cust == null)
                    {
                        sendResults.Add(new EmailSendResult
                        {
                            CustomerId = custId,
                            CustomerEmail = "(not found)",
                            Success = false,
                            ErrorMessage = "Customer not found"
                        });
                        continue;
                    }

                    // Build a small customer table for this recipient
                    string customerTableHtml = BuildCustomerTableHtml(cust);

                    // Combine user's message with the customer table (and a short header)
                    string htmlBody = $"<p>{vm.Message ?? ""}</p><h4>Your details</h4>{customerTableHtml}";

                    try
                    {
                        // bookingId is null for package related emails
                        await _emailService.SendEmailAndLogAsync(null, cust.Email, vm.Subject ?? "Package information", htmlBody, attachment, attachmentName);

                        sendResults.Add(new EmailSendResult
                        {
                            CustomerId = cust.CustomerId,
                            CustomerEmail = cust.Email,
                            Success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        // EmailService already logs the failure, but keep a local result too.
                        sendResults.Add(new EmailSendResult
                        {
                            CustomerId = cust.CustomerId,
                            CustomerEmail = cust.Email,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                // Show summary page / view
                return View("Result", sendResults);
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Failed to send emails: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private string BuildCustomerTableHtml(Customer cust)
        {
            // Simple 1-row HTML table with customer info
            return $@"
                <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse; width: 100%;'>
                    <thead>
                        <tr style='background:#efefef'>
                            <th>Name</th>
                            <th>Phone</th>
                            <th>Email</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>{cust.FirstName}</td>
                            <td>{cust.Phone}</td>
                            <td>{cust.Email}</td>
                        </tr>
                    </tbody>
                </table>";
        }
    }
}