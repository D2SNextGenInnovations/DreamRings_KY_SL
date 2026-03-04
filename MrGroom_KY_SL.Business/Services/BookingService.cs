using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Infrastructure;
using MrGroom_KY_SL.Business.CustomExceptions;
using MrGroom_KY_SL.Business.DTOs;

namespace MrGroom_KY_SL.Business.Services
{
    public class BookingService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IQueryable<Booking> GetAll()
        {
            return _unitOfWork.BookingRepository
                .GetAll()
                .Include(b => b.Customer)
                .Include(b => b.Package)
                .Include(b => b.BookingEventTypes.Select(be => be.EventType))
                .Include(b => b.Payments)
                .Include(b => b.StaffMembers);
        }

        public Booking GetById(int id)
        {
            return _unitOfWork.BookingRepository.GetAll()
                .Include(b => b.Customer)
                .Include(b => b.Package)
                .Include(b => b.Package.PackageItemPackages.Select(pip => pip.PackageItem))
                .Include(b => b.Package.PackagePhotos)

                .Include(b => b.BookingAddons.Select(a => a.PackageItem))

                .Include(b => b.BookingEventTypes.Select(be => be.EventType))
                .Include(b => b.Package.PackageEventTypes.Select(pet => pet.EventType))
                .Include(b => b.StaffMembers)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == id);
        }

        public Booking Create(Booking booking, int[] staffIds, List<BookingAddonDTO> addons = null)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            // Save booking
            _unitOfWork.BookingRepository.Insert(booking);
            _unitOfWork.Save();

            // Assign staff (OPTIONAL)
            if (staffIds != null && staffIds.Any())
            {
                var staffToAssign = _unitOfWork.StaffRepository
                    .GetAll()
                    .Where(s => staffIds.Contains(s.StaffId))
                    .ToList();

                foreach (var staff in staffToAssign)
                    booking.StaffMembers.Add(staff);

                _unitOfWork.Save();
            }

            // Load package data safely
            var packageEventTypeIds = new HashSet<int>();
            var packageItemIds = new HashSet<int>();

            Package package = null;

            if (booking.PackageId > 0)
            {
                package = _unitOfWork.PackageRepository
                    .GetAll()
                    .Include(p => p.PackageEventTypes)
                    .Include(p => p.PackageItemPackages.Select(pp => pp.PackageItem))
                    .FirstOrDefault(p => p.PackageId == booking.PackageId);

                if (package != null)
                {
                    packageEventTypeIds = package.PackageEventTypes
                        .Select(pe => pe.EventTypeId)
                        .ToHashSet();

                    packageItemIds = package.PackageItemPackages
                        .Where(pp => pp.PackageItem != null)
                        .Select(pp => pp.PackageItem.PackageItemId)
                        .ToHashSet();
                }
            }

            // Insert ALL selected EventTypes
            // merge EventTypes (KEEP existing, INSERT new only)
            if (booking.SelectedEventTypeIds != null && booking.SelectedEventTypeIds.Any())
            {
                // Existing event types for this booking
                var existingEventTypeIds = _unitOfWork.BookingEventTypeRepository
                    .GetAll()
                    .Where(x => x.BookingId == booking.BookingId)
                    .Select(x => x.EventTypeId)
                    .ToHashSet();

                // Insert ONLY new ones
                foreach (var eventTypeId in booking.SelectedEventTypeIds.Distinct())
                {
                    if (existingEventTypeIds.Contains(eventTypeId))
                        continue; //already exists, keep it

                    _unitOfWork.BookingEventTypeRepository.Insert(new BookingEventType
                    {
                        BookingId = booking.BookingId,
                        EventTypeId = eventTypeId
                    });
                }

                _unitOfWork.Save();
            }

            if (addons != null && addons.Any())
            {
                foreach (var addon in addons.Where(a => a.Quantity > 0))
                {
                    var item = _unitOfWork.PackageItemRepository
                        .GetById(addon.PackageItemId);

                    if (item == null) continue;

                    _unitOfWork.BookingAddonRepository.Insert(new BookingAddon
                    {
                        BookingId = booking.BookingId,
                        PackageItemId = item.PackageItemId,
                        Quantity = addon.Quantity,
                        UnitPrice = addon.UnitPrice > 0
                            ? addon.UnitPrice
                            : item.Price
                    });
                }
                _unitOfWork.Save();
            }
            return booking;
        }

        public void Update(Booking booking, int[] staffIds, List<BookingAddonDTO> addons = null)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking), "Booking object cannot be null.");

            // Load booking with relations
            var existing = _unitOfWork.BookingRepository
                .GetAll()
                .Include(b => b.StaffMembers)
                .Include(b => b.BookingEventTypes)
                .FirstOrDefault(b => b.BookingId == booking.BookingId);

            if (existing == null)
                throw new KeyNotFoundException($"Booking with ID {booking.BookingId} was not found.");

            // Update booking basic fields
            existing.CustomerId = booking.CustomerId;
            existing.PackageId = booking.PackageId;
            existing.EventDate = booking.EventDate;
            existing.Location = booking.Location;
            existing.Notes = booking.Notes;
            //existing.DiscountValue = booking.DiscountValue;
            //existing.DiscountPercentage = booking.DiscountPercentage;
            if (booking.DiscountValue.HasValue)
                existing.DiscountValue = booking.DiscountValue;
            if (booking.DiscountPercentage.HasValue)
                existing.DiscountPercentage = booking.DiscountPercentage;

            if (!string.IsNullOrWhiteSpace(booking.Status))
                existing.Status = booking.Status;

            existing.StaffMembers.Clear();

            if (staffIds != null && staffIds.Any())
            {
                var staffToAssign = _unitOfWork.StaffRepository
                    .GetAll()
                    .Where(s => staffIds.Contains(s.StaffId))
                    .ToList();

                foreach (var staff in staffToAssign)
                    existing.StaffMembers.Add(staff);
            }

            // Update Event Types (Many-to-Many)
            var selectedEventTypeIds = booking.SelectedEventTypeIds ?? Array.Empty<int>();

            // Get existing EventTypeIds for this booking
            var existingEventTypeIds = existing.BookingEventTypes
                .Select(be => be.EventTypeId)
                .ToHashSet();

            // Insert ONLY new event types
            foreach (var eventTypeId in selectedEventTypeIds)
            {
                if (existingEventTypeIds.Contains(eventTypeId))
                    continue;

                _unitOfWork.BookingEventTypeRepository.Insert(new BookingEventType
                {
                    BookingId = existing.BookingId,
                    EventTypeId = eventTypeId
                });
            }

            // Update Addons (EXTRA ONLY)
            var existingAddons = _unitOfWork.BookingAddonRepository
                .GetAll()
                .Where(a => a.BookingId == existing.BookingId)
                .ToList();

            // Load package item IDs
            var packageItemIds = new HashSet<int>();
            if (existing.PackageId > 0)
            {
                var package = _unitOfWork.PackageRepository
                    .GetAll()
                    .Include(p => p.PackageItemPackages)
                    .FirstOrDefault(p => p.PackageId == existing.PackageId);

                if (package != null)
                {
                    packageItemIds = package.PackageItemPackages
                        .Select(p => p.PackageItemId)
                        .ToHashSet();
                }
            }

            // Remove deleted addons
            foreach (var ex in existingAddons)
            {
                if (packageItemIds.Contains(ex.PackageItemId))
                    continue;

                if (addons == null || !addons.Any(a => a.PackageItemId == ex.PackageItemId))
                    _unitOfWork.BookingAddonRepository.Delete(ex);
            }

            // Add / Update addons
            if (addons != null)
            {
                foreach (var addon in addons.Where(a => a.Quantity > 0))
                {
                    if (packageItemIds.Contains(addon.PackageItemId))
                        continue;

                    var existingAddon = existingAddons
                        .FirstOrDefault(a => a.PackageItemId == addon.PackageItemId);

                    if (existingAddon != null)
                    {
                        existingAddon.Quantity = addon.Quantity;
                    }
                    else
                    {
                        var item = _unitOfWork.PackageItemRepository.GetById(addon.PackageItemId);
                        if (item == null) continue;

                        _unitOfWork.BookingAddonRepository.Insert(new BookingAddon
                        {
                            BookingId = existing.BookingId,
                            PackageItemId = item.PackageItemId,
                            Quantity = addon.Quantity,
                            UnitPrice = item.Price
                        });
                    }
                }
            }
            // Save all changes
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var existing = _unitOfWork.BookingRepository.GetById(id);
            if (existing != null)
            {
                _unitOfWork.BookingRepository.Delete(existing);
                _unitOfWork.Save();
            }
        }

        public void UpdateStatus(int bookingId, string status)
        {
            var booking = _unitOfWork.BookingRepository.GetById(bookingId);
            if (booking == null)
                throw new Exception("Booking not found");

            booking.Status = status;
            _unitOfWork.Save();
        }

        public void UpdateDiscount(int bookingId, decimal discountValue, decimal discountPercentage)
        {
            var booking = _unitOfWork.BookingRepository.GetById(bookingId);

            if (booking == null)
                throw new Exception("Booking not found");

            booking.DiscountValue = discountValue;
            booking.DiscountPercentage = discountPercentage;

            _unitOfWork.Save();
        }
    }
}
