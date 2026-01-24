using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Booking_EventTypes")]
    public class BookingEventType
    {
        [Key]
        public int BookingEventTypeId { get; set; }

        public int BookingId { get; set; }
        public int EventTypeId { get; set; }

        public virtual Booking Booking { get; set; }
        public virtual EventType EventType { get; set; }
    }
}
