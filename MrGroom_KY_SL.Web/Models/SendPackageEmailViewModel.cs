using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class SendPackageEmailViewModel
    {
        public int PackageId { get; set; }
        public string CustomerEmail { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}