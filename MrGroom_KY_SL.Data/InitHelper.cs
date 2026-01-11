using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Data
{
    public static class InitHelper
    {
        public static bool CreateInstanceForInit
        {
            get
            {
                using (var c = new AppDbContext())
                {
                    c.Database.Initialize(false);
                }
                return true;
            }
        }
    }
}
