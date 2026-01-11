using MrGroom_KY_SL.Business.Services;
using MrGroom_KY_SL.Models;
using MrGroom_KY_SL.Web.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [NoCache]
    public class CompanyInfoController : Controller
    {
        private readonly CompanyInfoService _companyInfoService = new CompanyInfoService();

        // GET: CompanyInfo
        public ActionResult Index()
        {
            try
            {
                var companyInfoList = _companyInfoService.GetAll();

                return View(companyInfoList); // Pass the list of companies to the view
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = "An error occurred while retrieving the company information.";
                TempData["ToastrType"] = "error";
                return View(new List<CompanyInfo>());
            }
        }

        // GET: CompanyInfo/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CompanyInfo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CompanyInfo model, HttpPostedFileBase logoFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Save the company info with the logo
                    _companyInfoService.Create(model, logoFile);

                    TempData["ToastrMessage"] = "Company Information added successfully!";
                    TempData["ToastrType"] = "success";
                    return RedirectToAction("Index");
                }

                // return the view with the current model
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["ToastrMessage"] = "An error occurred while saving the company information.";
                TempData["ToastrType"] = "error";
                return View(model);
            }
        }

        // GET: CompanyInfo/Edit/5
        public ActionResult Edit(int id)
        {
            try
            {
                var companyInfo = _companyInfoService.GetById(id);
                if (companyInfo == null)
                {
                    TempData["ToastrMessage"] = "Company Information not found!";
                    TempData["ToastrType"] = "error";
                    return RedirectToAction("Index");
                }
                return View(companyInfo);
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["ToastrMessage"] = "An error occurred while retrieving the company information for editing.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // POST: CompanyInfo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CompanyInfo model, HttpPostedFileBase logoFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Update the company info with the new logo if enter
                    _companyInfoService.Update(model, logoFile);

                    TempData["ToastrMessage"] = "Company Information updated successfully!";
                    TempData["ToastrType"] = "success";
                    return RedirectToAction("Index");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["ToastrMessage"] = "An error occurred while updating the company information.";
                TempData["ToastrType"] = "error";
                return View(model);
            }
        }

        // GET: CompanyInfo/Delete/5
        public ActionResult Delete(int id)
        {
            try
            {
                var companyInfo = _companyInfoService.GetById(id);
                if (companyInfo == null)
                {
                    TempData["ToastrMessage"] = "Company Information not found!";
                    TempData["ToastrType"] = "error";
                    return RedirectToAction("Index");
                }

                return View(companyInfo);
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["ToastrMessage"] = "An error occurred while retrieving the company information to delete.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }

        // POST: CompanyInfo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                _companyInfoService.Delete(id);

                TempData["ToastrMessage"] = "Company Information deleted successfully!";
                TempData["ToastrType"] = "success";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["ToastrMessage"] = "An error occurred while deleting the company information.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }
        }
    }
}