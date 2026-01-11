using MrGroom_KY_SL.Data.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class DashboardService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public int TotalBookings()
        {
            return _unitOfWork.BookingRepository.GetAll().Count();
        }

        public int TotalCustomers()
        {
            return _unitOfWork.CustomerRepository.GetAll().Count();
        }

        public decimal TotalRevenue()
        {
            return _unitOfWork.PaymentRepository
                .GetAll()
                .Sum(p => (decimal?)p.Amount) ?? 0m;
        }

        public int PendingPayments()
        {
            return _unitOfWork.PaymentRepository
                .GetAll()
                .Count(p => p.Status == "Pending");
        }
    }
}
