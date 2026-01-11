using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Package_Items")]
    public class PackageItem
    {
        [Key]
        public int PackageItemId { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Price { get; set; }

        public Nullable<bool> IsActive { get; set; } = true;
        // Many-to-many with Package
        public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
        public virtual ICollection<PackageItemPackage> PackageItemPackages { get; set; } = new List<PackageItemPackage>();


    }
}
