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
                .Include(b => b.EventType)
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
                     .Include(b => b.EventType)
                     .Include(b => b.Package.PackageEventTypes.Select(pet => pet.EventType))
                     .Include(b => b.StaffMembers)
                     .Include(b => b.Payments)
                     .FirstOrDefault(b => b.BookingId == id);
        }

        public Booking Create(Booking booking, int[] staffIds)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking), "Booking object cannot be null.");

            // -------------------------------------------------------
            // 1. Check duplicate booking on the same date for customer
            // -------------------------------------------------------
            var existingBooking = _unitOfWork.BookingRepository
                .GetAll()
                .Include(b => b.Payments)
                .Include(b => b.Package)
                .FirstOrDefault(b =>
                    b.CustomerId == booking.CustomerId &&
                    b.PackageId == booking.PackageId &&
                    DbFunctions.TruncateTime(b.EventDate) == DbFunctions.TruncateTime(booking.EventDate)
                );

            if (existingBooking != null)
            {
                decimal totalPaid = existingBooking.Payments.Sum(p => p.Amount);
                decimal price = existingBooking.Package.BasePrice;

                if (totalPaid < price)
                {
                    // Not fully paid → allow payment, block new booking
                    throw new ExistingUnpaidBookingWarningException(
                        "Customer already has a booking for this date. Please complete the pending payment.",
                        existingBooking.BookingId
                    );
                }

                // Fully paid duplicate → block creation
                throw new Exception("This booking already exists and is fully paid. Cannot create another booking for the same customer & date.");
            }

            // -------------------------------------------------------
            // 2. Prepare booking for insert
            // -------------------------------------------------------
            // Attach staff members
            if (staffIds != null && staffIds.Length > 0)
            {
                var staff = _unitOfWork.StaffRepository
                    .GetAll()
                    .Where(s => staffIds.Contains(s.StaffId))
                    .ToList();

                booking.StaffMembers = staff;
            }
            else
            {
                booking.StaffMembers = new List<Staff>();
            }

            booking.BookingDate = DateTime.UtcNow;

            // -------------------------------------------------------
            // 3. Insert booking into DB
            // -------------------------------------------------------
            _unitOfWork.BookingRepository.Insert(booking);
            _unitOfWork.Save();

            //if (staffIds != null && staffIds.Length > 0)
            //{
            //    foreach (var staffId in staffIds)
            //    {
            //        _unitOfWork.BookingStaffRepository.Insert(new BookingStaff
            //        {
            //            BookingId = booking.BookingId,
            //            StaffId = staffId
            //        });
            //    }

            //    _unitOfWork.Save();
            //}
            // After Save() → booking.BookingId is now populated by EF

            return booking;  // IMPORTANT — return saved booking with real ID
        }

        public void Update(Booking booking, int[] staffIds)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking), "Booking object cannot be null.");

            if (staffIds == null || staffIds.Length == 0)
                throw new ArgumentException("You must assign at least one staff member.", nameof(staffIds));

            // Load booking with staff
            var existing = _unitOfWork.BookingRepository
                .GetAll()
                .Include(b => b.StaffMembers)
                .FirstOrDefault(b => b.BookingId == booking.BookingId);

            if (existing == null)
                throw new KeyNotFoundException($"Booking with ID {booking.BookingId} was not found.");

            // -----------------------------
            // Update booking basic fields
            // -----------------------------
            existing.CustomerId = booking.CustomerId;
            existing.PackageId = booking.PackageId;
            existing.EventTypeId = booking.EventTypeId;
            existing.EventDate = booking.EventDate;
            existing.BookingDate = booking.BookingDate != DateTime.MinValue
                ? booking.BookingDate
                : existing.BookingDate;
            existing.Notes = booking.Notes;
            existing.Status = string.IsNullOrWhiteSpace(booking.Status)
                ? existing.Status
                : booking.Status;

            // -----------------------------
            // Update Staff (EF handles join table)
            // -----------------------------
            var staffToAssign = _unitOfWork.StaffRepository
                .GetAll()
                .Where(s => staffIds.Contains(s.StaffId))
                .ToList();

            if (!staffToAssign.Any())
                throw new InvalidOperationException("No matching staff members found.");

            // Clear old staff
            existing.StaffMembers.Clear();

            // Assign new staff
            foreach (var staff in staffToAssign)
            {
                existing.StaffMembers.Add(staff);
            }

            // Save changes
            _unitOfWork.Save();
        }

        //public void Update(Booking booking, int[] staffIds)
        //{
        //    try
        //    {
        //        if (booking == null)
        //            throw new ArgumentNullException(nameof(booking), "Booking object cannot be null.");

        //        if (staffIds == null || staffIds.Length == 0)
        //            throw new ArgumentException("You must assign at least one staff member.", nameof(staffIds));

        //        var existing = _unitOfWork.BookingRepository.GetAll()
        //            .Include(b => b.StaffMembers)
        //            .FirstOrDefault(b => b.BookingId == booking.BookingId);

        //        if (existing == null)
        //            throw new KeyNotFoundException($"Booking with ID {booking.BookingId} was not found.");

        //        // Update base info
        //        existing.CustomerId = booking.CustomerId;
        //        existing.PackageId = booking.PackageId;
        //        existing.EventTypeId = booking.EventTypeId;
        //        existing.EventDate = booking.EventDate;
        //        existing.BookingDate = booking.BookingDate != DateTime.MinValue
        //            ? booking.BookingDate
        //            : DateTime.UtcNow;
        //        existing.Notes = booking.Notes;
        //        existing.Status = string.IsNullOrWhiteSpace(booking.Status) ? "Pending" : booking.Status;

        //        // Update staff members
        //        var staffToAssign = _unitOfWork.StaffRepository
        //            .GetAll()
        //            .Where(s => staffIds.Contains(s.StaffId))
        //            .ToList();

        //        if (staffToAssign == null || staffToAssign.Count == 0)
        //            throw new InvalidOperationException("No matching staff members found for the given IDs.");

        //        //existing.StaffMembers = staffToAssign;
        //        // Remove old relations
        //        var oldRelations = _unitOfWork.BookingStaffRepository
        //                          .GetAll()
        //                          .Where(bs => bs.BookingId == existing.BookingId)
        //                          .ToList();

        //        foreach (var rel in oldRelations)
        //            _unitOfWork.BookingStaffRepository.Delete(rel);

        //        // Add new relations
        //        foreach (var staffId in staffIds)
        //        {
        //            _unitOfWork.BookingStaffRepository.Insert(new BookingStaff
        //            {
        //                BookingId = existing.BookingId,
        //                StaffId = staffId
        //            });
        //        }

        //        _unitOfWork.Save();
        //    }
        //    catch (ArgumentNullException)
        //    {
        //        // Let the caller handle argument exceptions
        //        throw;
        //    }
        //    catch (ArgumentException)
        //    {
        //        // Let the caller handle invalid argument exceptions
        //        throw;
        //    }
        //    catch (KeyNotFoundException)
        //    {
        //        // Let the caller handle not-found exceptions
        //        throw;
        //    }
        //    catch (DbEntityValidationException ex)
        //    {
        //        // Handles EF validation errors (if using EF6)
        //        var errors = ex.EntityValidationErrors
        //            .SelectMany(e => e.ValidationErrors)
        //            .Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
        //        var errorMessage = "Entity validation failed - " + string.Join("; ", errors);
        //        throw new Exception(errorMessage, ex);
        //    }
        //    catch (DbUpdateException ex)
        //    {
        //        // Handles EF update/database constraint errors
        //        throw new Exception("Database update failed while saving booking. Check relational constraints or data validity.", ex);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        // Handles invalid state in LINQ/EF operations
        //        throw new Exception("Invalid operation detected during booking update: " + ex.Message, ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Fallback for any unexpected errors
        //        throw new Exception("An unexpected error occurred while updating the booking: " + ex.Message, ex);
        //    }
        //}

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
    }
}
