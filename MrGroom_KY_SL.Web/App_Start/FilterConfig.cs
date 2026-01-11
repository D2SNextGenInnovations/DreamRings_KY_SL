using MrGroom_KY_SL.Web.Filters;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new NoCacheAttribute());
        }
    }
}
