using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Package_Photos")]
    public class PackagePhoto
    {
        [Key]
        public int PhotoId { get; set; }

        [ForeignKey("Package")]
        public int PackageId { get; set; }

        [Required, StringLength(255)]
        public string PhotoUrl { get; set; }

        [Range(1, 3)]
        public int DisplayOrder { get; set; }

        public virtual Package Package { get; set; }
    }
}
