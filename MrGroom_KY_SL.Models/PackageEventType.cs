using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Models
{
    [Table("Package_Event_Types")]
    public class PackageEventType
    {
        [Key]
        public int PackageEventTypeId { get; set; }

        [ForeignKey("Package")]
        public int PackageId { get; set; }

        [ForeignKey("EventType")]
        public int EventTypeId { get; set; }

        public decimal UnitPrice { get; set; }
        public virtual Package Package { get; set; }
        public virtual EventType EventType { get; set; }
    }
}
