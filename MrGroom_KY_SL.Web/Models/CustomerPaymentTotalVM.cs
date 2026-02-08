using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class CustomerPaymentTotalVM
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public int PaymentCount { get; set; }

        public List<CustomerPaymentRowVM> Payments { get; set; }
    }

    public class CustomerPaymentRowVM
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }

}