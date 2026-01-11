using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Packages")]
    public class Package
    {
        [Key]
        public int PackageId { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal")]
        public decimal BasePrice { get; set; }

        public int? DurationHours { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsIncludeSodftCopies { get; set; } = true;
        public int? EditedPhotosCount { get; set; }

        public virtual ICollection<PackageItem> PackageItems { get; set; } = new List<PackageItem>();
        public virtual ICollection<PackagePhoto> PackagePhotos { get; set; } = new List<PackagePhoto>();
        public virtual ICollection<PackageEventType> PackageEventTypes { get; set; } = new List<PackageEventType>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<PackageItemPackage> PackageItemPackages { get; set; } = new List<PackageItemPackage>();

    }
}
