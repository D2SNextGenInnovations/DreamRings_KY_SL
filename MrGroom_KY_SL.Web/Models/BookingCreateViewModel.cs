using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class BookingCreateViewModel
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public int? PackageId { get; set; }
        public DateTime? EventDate { get; set; }

        public string Location { get; set; }

        public int[] SelectedStaffIds { get; set; }
        public int[] SelectedEventTypeIds { get; set; }

        //ADDONS
        public List<BookingAddonViewModel> SelectedAddons { get; set; } = new List<BookingAddonViewModel>();
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountPercentage { get; set; }
    }
}