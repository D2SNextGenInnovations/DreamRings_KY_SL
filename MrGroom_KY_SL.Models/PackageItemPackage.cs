using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("PackageItemPackages")]
    public class PackageItemPackage
    {
        [Key]
        public int Id { get; set; }

        public int PackageId { get; set; }
        public int PackageItemId { get; set; }

        public int Qty { get; set; }
        public decimal CalculatedPrice { get; set; }
        public decimal UnitPrice { get; set; }

        [ForeignKey("PackageId")]
        public virtual Package Package { get; set; }

        [ForeignKey("PackageItemId")]
        public virtual PackageItem PackageItem { get; set; }
    }
}
