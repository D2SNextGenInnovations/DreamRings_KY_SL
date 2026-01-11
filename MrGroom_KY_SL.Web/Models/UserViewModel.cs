using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class UserViewModel
    {
        public int UserId { get; set; }
        [StringLength(50)]
        public string FirstName { get; set; }
        [StringLength(75)]
        public string LastName { get; set; }
        [StringLength(20)]
        public string Gender { get; set; }
        [Required ,StringLength(100)]
        public string Username { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required , EmailAddress]
        public string Email { get; set; }
        public string Role { get; set; }
        public byte[] Photo { get; set; }
        public HttpPostedFileBase PhotoFile { get; set; }
        public string PhotoBase64 { get; set; }
        public Nullable<DateTime> CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<DateTime> ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }
}