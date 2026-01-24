using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Booking_Addons")]
    public class BookingAddon
    {
        [Key]
        public int BookingAddonId { get; set; }

        [ForeignKey("Booking")]
        public int BookingId { get; set; }

        [ForeignKey("PackageItem")]
        public int PackageItemId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitPrice { get; set; }

        public virtual Booking Booking { get; set; }
        public virtual PackageItem PackageItem { get; set; }

        [NotMapped]
        public decimal Total => Quantity * UnitPrice;
    }
}
