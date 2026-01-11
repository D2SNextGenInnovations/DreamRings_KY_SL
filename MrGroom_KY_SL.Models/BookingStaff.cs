using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Booking_Staff")]
    public class BookingStaff
    {
        [Key]
        public int BookingStaffId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int StaffId { get; set; }

        [StringLength(50)]
        public string AssignedRole { get; set; }
        public virtual Booking Booking { get; set; }
        public virtual Staff Staff { get; set; }
    }
}
