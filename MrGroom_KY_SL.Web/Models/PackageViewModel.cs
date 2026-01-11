using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Models
{
    public class PackageViewModel
    {
        public int PackageId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Package Name")]
        public string Name { get; set; }

        [Display(Name = "Package Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Base Price")]
        public decimal BasePrice { get; set; }

        [Display(Name = "Duration (Hours)")]
        public int? DurationHours { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Include Soft Copies")]
        public bool IsIncludeSodftCopies { get; set; } = true;

        [Display(Name = "Edited Photos Count")]
        public int? EditedPhotosCount { get; set; }

        // ==============================
        // DATABASE LISTS (Raw Data)
        // ==============================
        public IEnumerable<PackageItem> PackageItemsFull { get; set; } = new List<PackageItem>();

        public IEnumerable<EventType> PackageEventsFull { get; set; } = new List<EventType>();

        // ==============================
        // SELECTED IDS (User Selected)
        // ==============================
        public List<int> SelectedPackageItems { get; set; } = new List<int>();

        public List<int> SelectedEventTypeIds { get; set; } = new List<int>();

        // ==============================
        // DROPDOWN / MULTI-SELECT LISTS
        // ==============================
        public IEnumerable<SelectListItem> PackageItems { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> EventTypes { get; set; } = new List<SelectListItem>();

        // ==============================
        // PACKAGE PHOTO MANAGEMENT
        // ==============================
        [Display(Name = "Upload Photos")]
        public List<HttpPostedFileBase> UploadedPhotos { get; set; } = new List<HttpPostedFileBase>();

        public List<PackagePhotoViewModel> PackagePhotos { get; set; } = new List<PackagePhotoViewModel>();
        public List<PackageItemSelectionVM> SelectedPackageItemDetails { get; set; }
        public List<PackageEventTypePacakge> SelectedPackageEventTypePacakgeDetails { get; set; }
    }

    public class PackagePhotoViewModel
    {
        public string PhotoUrl { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class PackageItemSelectionVM
    {
        public int PackageItemId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PackageEventTypePacakge
    {
        public int PacakgeEventTypeId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
