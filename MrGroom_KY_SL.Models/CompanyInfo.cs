using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("CompanyInfo")]
    public class CompanyInfo
    {
        [Key]
        public int CompanyInfoId { get; set; }

        [Required, StringLength(100)]
        public string CompanyName { get; set; }

        [StringLength(250)]
        public string Address { get; set; }

        [StringLength(50)]
        public string Phone { get; set; }

        [StringLength(50)]
        public string SecondaryPhone { get; set; }

        [StringLength(100)]
        public string Web { get; set; }

        [StringLength(100)]
        public string Fax { get; set; }

        [StringLength(50)]
        public string Email { get; set; }
        public byte[] CompanyLogo { get; set; } 
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }
}
