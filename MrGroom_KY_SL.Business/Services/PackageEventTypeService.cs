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
    public class PackageEventTypeService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<PackageEventType> GetAll()
        {
            return _unitOfWork.PackageEventTypeRepository
                .GetAll()
                .Include(pet => pet.Package)
                .Include(pet => pet.EventType)
                .ToList();
        }

        public PackageEventType GetById(int id)
        {
            return _unitOfWork.PackageEventTypeRepository
                .GetAll()
                .Include(pet => pet.Package)
                .Include(pet => pet.EventType)
                .FirstOrDefault(pet => pet.PackageEventTypeId == id);
        }

        public PackageEventType GetByIdDetails(int id)
        {
            try
            {
                var packageEventType = _unitOfWork.PackageEventTypeRepository
                    .GetAll()
                    .Include(pet => pet.Package)
                    .Include(pet => pet.EventType)
                    .FirstOrDefault(pet => pet.EventTypeId == id);

                if (packageEventType == null)
                {
                    return null;
                }

                return packageEventType;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching PackageEventType with ID {id}.", ex);
            }
        }

        public void Add(PackageEventType entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _unitOfWork.PackageEventTypeRepository.Insert(entity);
            _unitOfWork.Save();
        }

        public void Update(PackageEventType entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _unitOfWork.PackageEventTypeRepository.Update(entity);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var entity = _unitOfWork.PackageEventTypeRepository.GetById(id);

            if (entity == null)
                throw new KeyNotFoundException($"PackageEventType with ID {id} not found.");

            _unitOfWork.PackageEventTypeRepository.Delete(entity);
            _unitOfWork.Save();
        }
    }
}
