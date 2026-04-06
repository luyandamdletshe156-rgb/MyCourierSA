using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MyCourierSA.Startup))]
namespace MyCourierSA
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
