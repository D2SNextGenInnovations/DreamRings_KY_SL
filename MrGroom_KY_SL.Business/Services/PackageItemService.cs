using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class PackageItemService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<PackageItem> GetAll()
        {
            return _unitOfWork.PackageItemRepository.GetAll().Where(x => x.IsActive == true);//V3001
        }

        public PackageItem GetById(int id)
        {
            return _unitOfWork.PackageItemRepository.GetById(id);
        }

        public PackageItem GetById(int id, string includeProperties)
        {
            return _unitOfWork.PackageItemRepository.GetById(id, includeProperties);
        }

        public void Add(PackageItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _unitOfWork.PackageItemRepository.Insert(item);
            _unitOfWork.Save();
        }

        public void Update(PackageItem updatedItem)
        {
            if (updatedItem == null) throw new ArgumentNullException(nameof(updatedItem));

            var existingItem = _unitOfWork.PackageItemRepository.GetById(updatedItem.PackageItemId);
            if (existingItem == null)
                throw new KeyNotFoundException($"PackageItem with ID {updatedItem.PackageItemId} not found.");

            existingItem.Name = updatedItem.Name;
            existingItem.Description = updatedItem.Description;
            existingItem.Price = updatedItem.Price;
            existingItem.IsActive = updatedItem.IsActive;

            _unitOfWork.Save();
        }

        public PackageItem GetByIdWithPackages(int id)
        {
            return _unitOfWork.PackageItemRepository
                .GetAll(includeProperties:
                    "PackageItemPackages.Package,PackageItemPackages.Package.PackagePhotos")
                .FirstOrDefault(i => i.PackageItemId == id);
        }

        public void Delete(int id)
        {
            var entity = _unitOfWork.PackageItemRepository.GetById(id);
            //V3001
            if (entity == null)
                throw new KeyNotFoundException($"PackageItem with ID {id} not found.");

            // Soft delete instead of physical delete
            entity.IsActive = false;

            _unitOfWork.Save();
            //V3001
        }
    }
}
