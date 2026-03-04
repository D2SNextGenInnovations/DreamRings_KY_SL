using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    public class License
    {
        [Key]
        public int LicenseId { get; set; }

        [Required]
        public string ProductKey { get; set; }

        public DateTime ActivatedOn { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; }
    }
}
