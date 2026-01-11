using MrGroom_KY_SL.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MrGroom_KY_SL.Web.Filters
{
    public class CustomAuthorizeAttribute: ActionFilterAttribute
    {
        public string Role { get; set; }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var sessionUser = (SessionUser)filterContext.HttpContext.Session["User"];
            if (sessionUser == null)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
                return;
            }
            if (!string.IsNullOrEmpty(Role) && sessionUser.Role != Role)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
                return;
            }
            base.OnActionExecuting(filterContext);
        }
    }
}