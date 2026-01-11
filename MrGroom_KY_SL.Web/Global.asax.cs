using MrGroom_KY_SL.Data;
using MrGroom_KY_SL.Web.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;


namespace MrGroom_KY_SL.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;


            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            var ensure = InitHelper.CreateInstanceForInit;

            // Apply migrations automatically
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Data.Migrations.Configuration>());

            using (var context = new AppDbContext())
            {
                context.Database.Initialize(force: true);
            }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context == null) return;

            var authCookie = context.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                try
                {
                    var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (ticket != null && !ticket.Expired)
                    {
                        var roles = ticket.UserData.Split(',');
                        context.User = new System.Security.Principal.GenericPrincipal(
                            new System.Security.Principal.GenericIdentity(ticket.Name),
                            roles
                        );
                    }
                }
                catch
                {
                    FormsAuthentication.SignOut();
                }
            }
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            // Redirect unauthorized (401) responses to login page
            if (Response.StatusCode == 401)
            {
                Response.ClearContent();
                Response.Redirect("~/Account/Login");
            }
        }

    }
}
