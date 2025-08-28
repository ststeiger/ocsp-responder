
namespace ReportServerProxyFF
{


    public class Global 
        : System.Web.HttpApplication
    {

        public static System.Web.IHttpModule ProxyModule = new ReportProxyModule();


        public override void Init()
        {
            SslHack.InitiateSSLTrust();
            base.Init();
            ProxyModule.Init(this);
        }



        protected void Application_Start(object sender, System.EventArgs e)
        {
            SslHack.InitiateSSLTrust();
        }

        protected void Session_Start(object sender, System.EventArgs e)
        { }

        protected void Application_BeginRequest(object sender, System.EventArgs e)
        { }

        protected void Application_AuthenticateRequest(object sender, System.EventArgs e)
        { }

        protected void Application_Error(object sender, System.EventArgs e)
        { }

        protected void Session_End(object sender, System.EventArgs e)
        { }

        protected void Application_End(object sender, System.EventArgs e)
        { }


    }
}