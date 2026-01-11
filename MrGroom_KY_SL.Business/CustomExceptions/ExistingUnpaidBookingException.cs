using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.CustomExceptions
{
    public class ExistingUnpaidBookingException : Exception
    {
        public int ExistingBookingId { get; }

        public ExistingUnpaidBookingException(string message, int bookingId) : base(message)
        {
            ExistingBookingId = bookingId;
        }
    }
}
