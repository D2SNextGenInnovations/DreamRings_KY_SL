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
            return _unitOfWork.PackageItemRepository.GetAll();
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

        public void Update(PackageItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _unitOfWork.PackageItemRepository.Update(item);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var entity = _unitOfWork.PackageItemRepository.GetById(id);
            if (entity == null) throw new KeyNotFoundException($"PackageItem with ID {id} not found.");

            _unitOfWork.PackageItemRepository.Delete(entity);
            _unitOfWork.Save();
        }
    }
}
