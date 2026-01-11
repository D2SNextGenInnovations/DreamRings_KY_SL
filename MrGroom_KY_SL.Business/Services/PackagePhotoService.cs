using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class PackagePhotoService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<PackagePhoto> GetAll()
        {
            return _unitOfWork.PackagePhotoRepository
                .GetAllQuery()
                .Include(p => p.Package)
                .ToList();
        }

        public PackagePhoto GetById(int id)
        {
            return _unitOfWork.PackagePhotoRepository
                .GetAllQuery()
                .Include(p => p.Package)
                .FirstOrDefault(p => p.PhotoId == id);
        }

        public void Add(PackagePhoto photo)
        {
            if (photo == null)
                throw new ArgumentNullException(nameof(photo));

            _unitOfWork.PackagePhotoRepository.Insert(photo);
            _unitOfWork.Save();
        }

        public void Update(PackagePhoto photo)
        {
            if (photo == null)
                throw new ArgumentNullException(nameof(photo));

            _unitOfWork.PackagePhotoRepository.Update(photo);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var entity = _unitOfWork.PackagePhotoRepository.GetById(id);

            if (entity == null)
                throw new KeyNotFoundException($"PackagePhoto with ID {id} not found.");

            _unitOfWork.PackagePhotoRepository.Delete(entity);
            _unitOfWork.Save();
        }
    }
}
