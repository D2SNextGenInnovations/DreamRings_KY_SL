using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class BookingPaymentVM
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalBookingAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance => TotalBookingAmount - PaidAmount;
        public string PaymentType { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentStatus
        {
            get
            {
                if (PaidAmount == 0)
                    return "none";
                if (PaidAmount == 10000)
                    return "Advance";
                if (PaidAmount > 10000 && PaidAmount < TotalBookingAmount / 2)
                    return "half";
                return "Full";
            }
        }
    }
}