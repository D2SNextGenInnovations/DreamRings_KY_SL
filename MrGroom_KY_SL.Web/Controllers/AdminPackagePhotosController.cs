using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [NoCache]
    public class AdminPackagePhotosController : Controller
    {
        private readonly PackagePhotoService _packagePhotoService = new PackagePhotoService();
        private readonly PackageService _packageService = new PackageService();

        public ActionResult Index() => View(_packagePhotoService.GetAll());

        public ActionResult Create()
        {
            var packages = _packageService.GetAll();
            ViewBag.Packages = new SelectList(packages, "PackageId", "Name");
            return View(new PackagePhoto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HttpPostedFileBase photoFile, PackagePhoto photo)
        {
            ViewBag.Packages = new SelectList(_packageService.GetAll(), "PackageId", "Name", photo.PackageId);

            if (photoFile == null || photoFile.ContentLength == 0)
            {
                ModelState.AddModelError("PhotoUrl", "Please select a photo to upload.");
                return View(photo);
            }

            string uploadPathSetting = ConfigurationManager.AppSettings["PackagePhotoUploadPath"];
            string uploadDir = !string.IsNullOrEmpty(uploadPathSetting)
                ? Server.MapPath(uploadPathSetting)
                : Server.MapPath("~/Uploads/PackagePhotos");

            // Ensure upload folder exists
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            // Save file to /Uploads/PackagePhotos
            var fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
            var filePath = Path.Combine(uploadDir, fileName);
            photoFile.SaveAs(filePath);

            // Save file path in DB
            photo.PhotoUrl = "/Uploads/PackagePhotos/" + fileName;

            _packagePhotoService.Add(photo);

            TempData["SuccessMessage"] = "Photo added successfully!";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var photo = _packagePhotoService.GetById(id);
            if (photo == null) return HttpNotFound();

            ViewBag.Packages = new SelectList(_packageService.GetAll(), "PackageId", "Name", photo.PackageId);
            return View(photo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PackagePhoto photo)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Packages = new SelectList(_packageService.GetAll(), "PackageId", "Name", photo.PackageId);
                return View(photo);
            }

            _packagePhotoService.Update(photo);
            return RedirectToAction("Index");
        }

        //public ActionResult Delete(int id) => View(_packagePhotoService.GetById(id));
        public ActionResult Delete(int id)
        {
            var photo = _packagePhotoService.GetById(id);
            if (photo == null) return HttpNotFound();
            return View(photo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            _packagePhotoService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}