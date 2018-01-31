using System;
using log4net.Config;
using log4net;

namespace PasswordReset.SelfService
{
    public class Global : System.Web.HttpApplication
    {
        private static ILog log = LogManager.GetLogger(typeof(Global));

        protected void Application_Start(object sender, EventArgs e)
        {
            XmlConfigurator.Configure();
            log.Info("Starting Application...");
            helpers.DBUtil.EnsureTables();
            helpers.DBUtil.CleanupTrackers();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}