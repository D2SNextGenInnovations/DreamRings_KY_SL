using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Staff")]
    public class Staff
    {
        [Key]
        public int StaffId { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Role { get; set; } // Photographer, Editor, Assistant

        [StringLength(20)]
        public string Phone { get; set; }

        [EmailAddress, StringLength(100)]
        public string Email { get; set; }

        public Nullable<bool> IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Many-to-many
        public virtual ICollection<Booking> Bookings { get; set; }
    }
}
