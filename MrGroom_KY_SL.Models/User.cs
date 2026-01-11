using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(75)]
        public string LastName { get; set; }

        [StringLength(20)]
        public string Gender { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; }

        [Required, StringLength(200)]
        public string Password { get; set; }

        [Required, StringLength(200)]
        public string Email { get; set; }

        [Required, StringLength(20)]
        public string Role { get; set; }

        public byte[] Photo { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<DateTime> CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }
}
