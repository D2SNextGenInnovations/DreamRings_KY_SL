using MrGroom_KY_SL.Data.Repository;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Data.UnitOfWork
{
    public class UnitOfWork : IDisposable
    {
        private readonly AppDbContext _context;
        private Repository<User> _userRepository;
        private Repository<Customer> _customerRepository;
        private Repository<Package> _packageRepository;
        private Repository<EventType> _eventTypeRepository;
        private Repository<Booking> _bookingRepository;
        private Repository<Payment> _paymentRepository;
        private Repository<Staff> _staffRepository;
        private Repository<BookingStaff> _bookingStaffRepository;
        private Repository<PackageEventType> _packageEventTypeRepository;
        private Repository<PackageItem> _packageItemRepository;
        private Repository<PackagePhoto> _packagePhotoRepository;
        private Repository<EmailHistory> _emailHistoryRepository;
        private Repository<CompanyInfo> _companyInfoRepository;

        public UnitOfWork()
        {
            _context = new AppDbContext();
        }

        public Repository<User> UserRepository => _userRepository ?? (_userRepository = new Repository<User>(_context));
        public Repository<Customer> CustomerRepository => _customerRepository ?? (_customerRepository = new Repository<Customer>(_context));
        public Repository<Package> PackageRepository => _packageRepository ?? (_packageRepository = new Repository<Package>(_context));
        public Repository<EventType> EventTypeRepository => _eventTypeRepository ?? (_eventTypeRepository = new Repository<EventType>(_context));
        public Repository<Booking> BookingRepository => _bookingRepository ?? (_bookingRepository = new Repository<Booking>(_context));
        public Repository<Payment> PaymentRepository => _paymentRepository ?? (_paymentRepository = new Repository<Payment>(_context));
        public Repository<Staff> StaffRepository => _staffRepository ?? (_staffRepository = new Repository<Staff>(_context));
        public Repository<BookingStaff> BookingStaffRepository => _bookingStaffRepository ?? (_bookingStaffRepository = new Repository<BookingStaff>(_context));
        public Repository<PackageEventType> PackageEventTypeRepository => _packageEventTypeRepository ?? (_packageEventTypeRepository = new Repository<PackageEventType>(_context));
        public Repository<PackageItem> PackageItemRepository => _packageItemRepository ?? (_packageItemRepository = new Repository<PackageItem>(_context));
        public Repository<PackagePhoto> PackagePhotoRepository => _packagePhotoRepository ?? (_packagePhotoRepository = new Repository<PackagePhoto>(_context));
        public Repository<EmailHistory> EmailHistoryRepository => _emailHistoryRepository ?? (_emailHistoryRepository = new Repository<EmailHistory>(_context));
        public Repository<CompanyInfo> CompanyInfoRepository => _companyInfoRepository ?? (_companyInfoRepository = new Repository<CompanyInfo>(_context));

        public AppDbContext Context => _context;
        public void Save() => _context.SaveChanges();
        public void Dispose() => _context.Dispose();
    }
}
