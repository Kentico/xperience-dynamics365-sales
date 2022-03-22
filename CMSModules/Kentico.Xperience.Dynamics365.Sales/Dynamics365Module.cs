using CMS;
using CMS.DataEngine;

using Kentico.Xperience.Dynamics365.Sales;
using Kentico.Xperience.Dynamics365.Sales.Controllers;

using System.Web.Http;

[assembly: RegisterModule(typeof(Dynamics365Module))]
namespace Kentico.Xperience.Dynamics365.Sales
{
    /// <summary>
    /// A custom module used to initialize the routes used in <see cref="Dynamics365ContactController"/>.
    /// </summary>
    public class Dynamics365Module : Module
    {
        public Dynamics365Module() : base(nameof(Dynamics365Module))
        {

        }


        protected override void OnInit()
        {
            base.OnInit();

            GlobalConfiguration.Configuration.Routes.MapHttpRoute(
                "dynamics365",
                "dynamics365/updatecontact",
                defaults: new { controller = "Dynamics365Contact", action = "Update" }
            );
        }
    }
}