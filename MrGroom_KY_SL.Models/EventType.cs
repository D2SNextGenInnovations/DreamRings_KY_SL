using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Event_Types")]
    public class EventType
    {
        [Key]
        public int EventTypeId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Price { get; set; }
        public Nullable<bool> IsActive { get; set; } = true;

        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<PackageEventType> PackageEventTypes { get; set; } = new List<PackageEventType>();
    }
}
