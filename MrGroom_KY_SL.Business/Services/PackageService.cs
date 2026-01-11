using MrGroom_KY_SL.Data;
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
    public class PackageService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<Package> GetAll()
        {
            return _unitOfWork.PackageRepository.GetAll(includeProperties: "PackageItems,PackagePhotos,PackageEventTypes.EventType");
        }

        public Package GetById(int id)
        {
            return _unitOfWork.PackageRepository
                .GetAll(includeProperties: "PackageItems,PackagePhotos,PackageEventTypes.EventType")
                .FirstOrDefault(p => p.PackageId == id);
        }

        public Package GetByIdWithDetails(int id)
        {
            return _unitOfWork.PackageRepository
                .GetAll(includeProperties: "PackageItemPackages.PackageItem,PackagePhotos,PackageEventTypes.EventType")
                .FirstOrDefault(p => p.PackageId == id);
        }

        /// This overload supports includeProperties — required for your controller call.
        public Package GetById(int id, string includeProperties)
        {
            return _unitOfWork.PackageRepository.GetById(id, includeProperties);
        }

        public void Create(Package package)
        {
            _unitOfWork.PackageRepository.Insert(package);
            _unitOfWork.Save();
        }

        public void Update(Package package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            using (var context = new AppDbContext())
            {
                // Load existing package including relationships
                var existing = context.Packages
                    .Include(p => p.PackageItems)
                    .Include(p => p.PackageEventTypes)
                    .Include(p => p.PackagePhotos)
                    .FirstOrDefault(p => p.PackageId == package.PackageId);

                if (existing == null)
                    throw new KeyNotFoundException($"Package with ID {package.PackageId} not found.");

                // Update scalar fields 
                existing.Name = package.Name;
                existing.Description = package.Description;
                existing.BasePrice = package.BasePrice;
                existing.DurationHours = package.DurationHours;
                existing.IsActive = package.IsActive;

                //Update PackageItems (many-to-many) 
                var newItemIds = package.PackageItems?.Select(i => i.PackageItemId).ToList() ?? new List<int>();
                var currentItemIds = existing.PackageItems.Select(i => i.PackageItemId).ToList();

                // Remove old items
                foreach (var item in existing.PackageItems.Where(i => !newItemIds.Contains(i.PackageItemId)).ToList())
                    existing.PackageItems.Remove(item);

                // Add new items
                foreach (var id in newItemIds.Except(currentItemIds))
                {
                    var trackedItem = context.PackageItems.Find(id);
                    if (trackedItem != null)
                        existing.PackageItems.Add(trackedItem);
                }

                // Update PackageEventTypes
                var newEventIds = package.PackageEventTypes?.Select(e => e.EventTypeId).ToList() ?? new List<int>();
                var currentEventIds = existing.PackageEventTypes.Select(e => e.EventTypeId).ToList();

                // Remove old
                foreach (var evt in existing.PackageEventTypes.Where(e => !newEventIds.Contains(e.EventTypeId)).ToList())
                    context.PackageEventTypes.Remove(evt);

                // Add new
                foreach (var evtId in newEventIds.Except(currentEventIds))
                {
                    existing.PackageEventTypes.Add(new PackageEventType
                    {
                        PackageId = existing.PackageId,
                        EventTypeId = evtId
                    });
                }

                // Update PackagePhotos 
                // Remove all old photos
                foreach (var photo in existing.PackagePhotos.ToList())
                    context.PackagePhotos.Remove(photo);

                // Add new ones (if any)
                if (package.PackagePhotos != null && package.PackagePhotos.Any())
                {
                    int order = 1;
                    foreach (var photo in package.PackagePhotos)
                    {
                        if (!string.IsNullOrWhiteSpace(photo.PhotoUrl))
                        {
                            existing.PackagePhotos.Add(new PackagePhoto
                            {
                                PackageId = existing.PackageId,
                                PhotoUrl = photo.PhotoUrl,
                                DisplayOrder = photo.DisplayOrder > 0 ? photo.DisplayOrder : order++
                            });
                        }
                    }
                }

                // Save 
                try
                {
                    context.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    var errors = ex.EntityValidationErrors
                        .SelectMany(e => e.ValidationErrors)
                        .Select(e => $"Property: {e.PropertyName}, Error: {e.ErrorMessage}")
                        .ToList();

                    var detailedMessage = "Validation failed: " + string.Join("; ", errors);
                    throw new Exception(detailedMessage, ex);
                }
            }
        }

        public void Delete(int id)
        {
            var package = _unitOfWork.PackageRepository.GetById(id);
            if (package != null)
            {
                _unitOfWork.PackageRepository.Delete(package);
                _unitOfWork.Save();
            }
        }
    }
}
