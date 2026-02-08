using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class PaymentViewModel
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentType { get; set; }
        public string Remarks { get; set; }
    }
}