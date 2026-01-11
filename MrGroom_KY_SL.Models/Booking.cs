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

        [ForeignKey("EventType")]
        [Required(ErrorMessage = "Event type is required")]
        public int EventTypeId { get; set; }

        public virtual EventType EventType { get; set; }

        [ForeignKey("Package")]
        [Required(ErrorMessage = "Package is required")]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; }
        public virtual Package Package { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        public DateTime EventDate { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        public string Notes { get; set; }

        public virtual ICollection<Payment> Payments { get; set; }


        [NotMapped]
        [Display(Name = "Assigned Staff")]
        public int[] SelectedStaffIds { get; set; }

        public virtual ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();

        [NotMapped]
        public decimal TotalPaid => Payments?.Sum(p => p.Amount) ?? 0;
        [NotMapped]
        public decimal RemainingAmount => (Package?.BasePrice ?? 0) - (Payments?.Sum(p => p.Amount) ?? 0);
        [NotMapped]
        public string PaymentStatus
        {
            get
            {
                decimal packagePrice = Package?.BasePrice ?? 0;
                decimal paid = Payments?.Sum(p => p.Amount) ?? 0;

                if (paid == 0) return "Unpaid";
                if (paid < packagePrice / 2) return "Advance";
                if (paid < packagePrice) return "Half";
                if (paid >= packagePrice) return "Full";
                return "Partial";
            }
        }

    }
}
