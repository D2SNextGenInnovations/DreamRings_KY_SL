using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MrGroom_KY_SL.Web.Models
{
    public class CustomerViewModel
    {
        public int CustomerId { get; set; }

        [Required, StringLength(200)]
        public string FirstName { get; set; }

        [StringLength(200)]
        public string LastName { get; set; }

        [EmailAddress, StringLength(200)]
        public string Email { get; set; }

        [StringLength(50)]
        public string Phone { get; set; }

        [StringLength(50)]
        public string NICNumber { get; set; }

        // Address fields
        [Required, StringLength(200)]
        public string AddressLine1 { get; set; }

        [StringLength(200)]
        public string AddressLine2 { get; set; }

        [StringLength(200)]
        public string AddressLine3 { get; set; }

        [Required, StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string StateOrProvince { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [Required, StringLength(100)]
        public string Country { get; set; }

        [StringLength(50)]
        public string AddressType { get; set; }
    }
}