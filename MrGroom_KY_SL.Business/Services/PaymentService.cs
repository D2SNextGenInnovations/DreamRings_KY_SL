using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class PaymentService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<Payment> GetAllByBooking(int bookingId)
        {
            return _unitOfWork.PaymentRepository
                .GetAll()
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();
        }

        public Payment GetById(int id)
        {
            return _unitOfWork.PaymentRepository.GetById(id);
        }

        public void AddPayment(Payment payment)
        {
            if (payment.BookingId <= 0)
                throw new Exception("BookingId was not set before saving payment.");

            _unitOfWork.PaymentRepository.Insert(payment);
            _unitOfWork.Save();
        }

        public void Update(Payment payment)
        {
            _unitOfWork.PaymentRepository.Update(payment);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var p = _unitOfWork.PaymentRepository.GetById(id);
            if (p != null)
            {
                _unitOfWork.PaymentRepository.Delete(p);
                _unitOfWork.Save();
            }
        }
    }
}
