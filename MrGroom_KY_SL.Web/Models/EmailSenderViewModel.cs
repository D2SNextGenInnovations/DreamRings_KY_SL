using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Models
{
    public enum EmailSendMode
    {
        SinglePackage = 0,
        AllPackages = 1
    }

    public class EmailSenderViewModel
    {
        public EmailSendMode SendMode { get; set; }

        // for SinglePackage mode
        public int? PackageId { get; set; }

        // Selected customer ids (checkboxes)
        public List<int> SelectedCustomerIds { get; set; } = new List<int>();

        public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Packages { get; set; } = new List<SelectListItem>();

        public string Subject { get; set; }
        public string Message { get; set; }
    }

    public class EmailSendResult
    {
        public int CustomerId { get; set; }
        public string CustomerEmail { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}