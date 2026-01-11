using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.CustomExceptions
{
    public class ExistingUnpaidBookingWarningException : Exception
    {
        public int ExistingBookingId { get; }

        public ExistingUnpaidBookingWarningException(string message, int existingBookingId)
            : base(message)
        {
            ExistingBookingId = existingBookingId;
        }
    }
}
