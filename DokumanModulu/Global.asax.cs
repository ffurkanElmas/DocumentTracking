using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using DokumanModulu.Entity;
using DokumanModulu.Identity;

namespace DokumanModulu
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            
            Database.SetInitializer(new DataInitializer());
            Database.SetInitializer(new IdentityInitializer());

           
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var context = new IdentityDataContext())
            {
                context.Database.Initialize(force: true);
            }

            using (var context = new DataContext())
            {
                context.Database.Initialize(force: true);
            }
        }
    }
}
