using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SaigonRide.Startup))]
namespace SaigonRide
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
