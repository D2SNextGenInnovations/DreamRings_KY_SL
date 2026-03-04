using MrGroom_KY_SL.Business.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MrGroom_KY_SL.Web.Filters
{
    public class LicenseCheckAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool skip =
                filterContext.ActionDescriptor.IsDefined(typeof(SkipLicenseCheckAttribute), true)
                ||
                filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(SkipLicenseCheckAttribute), true);

            if (skip)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            var licenseService = new LicenseService();

            if (!licenseService.IsLicenseValid())
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "License", action = "Activate" }));
            }

            base.OnActionExecuting(filterContext);
        }
    }
}