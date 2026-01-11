using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("EmailHistory")]
    public class EmailHistory
    {
        [Key]
        public int EmailHistoryId { get; set; }

        public int? BookingId { get; set; }

        [Required]
        [StringLength(200)]
        public string RecipientEmail { get; set; }

        [Required]
        [StringLength(300)]
        public string Subject { get; set; }

        public string MessageBody { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } 

        public string ErrorMessage { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }
    }
}
