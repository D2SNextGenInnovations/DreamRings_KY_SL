using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Bookings")]
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [ForeignKey("Customer")]
        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }

        public virtual Customer Customer { get; set; }

        [ForeignKey("Package")]
        [Required(ErrorMessage = "Package is required")]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; }
        public virtual Package Package { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        //public DateTime EventDate { get; set; }
        public DateTime? EventDate { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        [StringLength(50)]
        public string PreviousStatus { get; set; }

        public string Notes { get; set; }

        public virtual ICollection<Payment> Payments { get; set; }

        [NotMapped]
        [Display(Name = "Assigned Staff")]
        public int[] SelectedStaffIds { get; set; }

        public virtual ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();

        [NotMapped]
        public decimal TotalPaid => Payments?.Sum(p => p.Amount) ?? 0;

        [NotMapped]
        public decimal EventTypesTotal => BookingEventTypes?.Sum(e => e.EventType.Price) ?? 0;

        [NotMapped]
        public decimal GrandTotal => (Package?.BasePrice ?? 0) + EventTypesTotal + AddonsTotal;

        [NotMapped]
        public decimal RemainingAmount => GrandTotal - (Payments?.Sum(p => p.Amount) ?? 0);


        [NotMapped]
        public string PaymentStatus
        {
            get
            {
                decimal total = GrandTotal;
                decimal paid = Payments?.Sum(p => p.Amount) ?? 0;

                if (paid == 0) return "Unpaid";
                if (paid < total / 2) return "Advance";
                if (paid < total) return "Half";
                return "Full";

            }
        }

        public virtual ICollection<BookingEventType> BookingEventTypes { get; set; } = new List<BookingEventType>();

        [NotMapped]
        public int[] SelectedEventTypeIds { get; set; }

        public virtual ICollection<BookingAddon> BookingAddons { get; set; } = new List<BookingAddon>();

        [NotMapped]
        public decimal AddonsTotal => BookingAddons?.Sum(a => a.Quantity * a.UnitPrice) ?? 0;
    }
}
