using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MrGroom_KY_SL.Business.Services
{
    public class CompanyInfoService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<CompanyInfo> GetAll() => _unitOfWork.CompanyInfoRepository.GetAll();

        public CompanyInfo GetById(int id) => _unitOfWork.CompanyInfoRepository.GetById(id);

        public void Create(CompanyInfo companyInfo, HttpPostedFileBase logoFile)
        {
            if (logoFile != null && logoFile.ContentLength > 0)
            {
                using (var ms = new MemoryStream())
                {
                    logoFile.InputStream.CopyTo(ms);
                    companyInfo.CompanyLogo = ms.ToArray();
                }
            }

            // Add audit fields
            companyInfo.CreatedOn = DateTime.Now;
            companyInfo.CreatedBy = HttpContext.Current.User.Identity.Name;

            _unitOfWork.CompanyInfoRepository.Insert(companyInfo);
            _unitOfWork.Save();
        }

        public void Update(CompanyInfo model, HttpPostedFileBase logoFile)
        {
            var existing = _unitOfWork.CompanyInfoRepository.GetById(model.CompanyInfoId);

            if (existing == null)
                throw new Exception("Company record not found.");

            // Update only allowed fields
            existing.CompanyName = model.CompanyName;
            existing.Address = model.Address;
            existing.Phone = model.Phone;
            existing.SecondaryPhone = model.SecondaryPhone;
            existing.Web = model.Web;
            existing.Fax = model.Fax;
            existing.Email = model.Email;

            // Update logo if new uploaded
            if (logoFile != null && logoFile.ContentLength > 0)
            {
                using (var ms = new MemoryStream())
                {
                    logoFile.InputStream.CopyTo(ms);
                    existing.CompanyLogo = ms.ToArray();
                }
            }

            // Preserve audit fields
            existing.ModifiedOn = DateTime.Now;
            existing.ModifiedBy = HttpContext.Current.User.Identity.Name;

            _unitOfWork.CompanyInfoRepository.Update(existing);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var companyInfo = _unitOfWork.CompanyInfoRepository.GetById(id);
            if (companyInfo != null)
            {
                _unitOfWork.CompanyInfoRepository.Delete(companyInfo);
                _unitOfWork.Save();
            }
        }
    }
}
