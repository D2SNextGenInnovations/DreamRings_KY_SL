using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingPayments { get; set; }
        public List<UserViewModel> Users { get; set; }
    }
}