using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class StaffService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<Staff> GetAll()
        {
            return _unitOfWork.StaffRepository.GetAll().ToList();
        }

        public Staff GetById(int id)
        {
            return _unitOfWork.StaffRepository.GetById(id);
        }

        public Staff GetByIdWithBookings(int id)
        {
            return _unitOfWork.StaffRepository
                .Get(
                    filter: s => s.StaffId == id,
                    includeProperties: "Bookings,Bookings.Customer,Bookings.EventType,Bookings.Package,Bookings.Payments"
                )
                .FirstOrDefault();
        }

        public void Create(Staff staff)
        {
            _unitOfWork.StaffRepository.Insert(staff);
            _unitOfWork.Save();
        }

        public void Update(Staff staff)
        {
            _unitOfWork.StaffRepository.Update(staff);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var existing = _unitOfWork.StaffRepository.GetById(id);
            if (existing != null)
            {
                _unitOfWork.StaffRepository.Delete(existing);
                _unitOfWork.Save();
            }
        }
    }
}
