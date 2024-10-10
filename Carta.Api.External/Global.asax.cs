using Carta.Api.External.Dal.Cache;
using Carta.Api.External.Logger;
using log4net.Repository.Hierarchy;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace Carta.Api.External
{
    public class Global : System.Web.HttpApplication
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected void Application_Start(object sender, EventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();
            log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            log.Info("Application started");
            var repository = LogManager.GetRepository();
            var appenders = repository.GetAppenders();

            var maskingAppender = appenders.FirstOrDefault(a => a.Name == "MaskingAppender") as MaskingAppenderSkeleton;
            if (maskingAppender != null)
            {
                var originalAppender = appenders.FirstOrDefault(a => a.Name == "RollingLogFileAppender");
                if (originalAppender != null)
                {
                    maskingAppender.AddAppender(originalAppender);
                }
            }
            CacheContainer.Init();
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            string sessionId = Session.SessionID;
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