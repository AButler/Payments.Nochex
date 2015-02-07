using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.Nochex {
  public class RouteProvider: IRouteProvider {
    public void RegisterRoutes( RouteCollection routes ) {

      routes.MapRoute( 
        "Plugin.Payments.Nochex.Configure", 
        "Plugins/PaymentNochex/Configure", 
        new { controller = "PaymentNochex", action = "Configure" }, 
        new[] { "Nop.Plugin.Payments.Nochex.Controllers" } 
      );

      routes.MapRoute(
        "Plugin.Payments.Nochex.PaymentInfo", 
        "Plugins/PaymentNochex/PaymentInfo", 
        new { controller = "PaymentNochex", action = "PaymentInfo" }, 
        new[] { "Nop.Plugin.Payments.Nochex.Controllers" } 
      );

      routes.MapRoute( 
        "Plugin.Payments.Nochex.APCHandler", 
        "Plugins/PaymentNochex/APCHandler", 
        new { controller = "PaymentNochex", action = "APCHandler" }, 
        new[] { "Nop.Plugin.Payments.Nochex.Controllers" } 
      );

      routes.MapRoute( 
        "Plugin.Payments.Nochex.CancelOrder", 
        "Plugins/PaymentNochex/CancelOrder", 
        new { controller = "PaymentNochex", action = "CancelOrder" }, 
        new[] { "Nop.Plugin.Payments.Nochex.Controllers" } 
      );

      routes.MapRoute( 
        "Plugin.Payments.Nochex.SuccessOrder", 
        "Plugins/PaymentNochex/SuccessOrder", 
        new { controller = "PaymentNochex", action = "SuccessOrder" }, 
        new[] { "Nop.Plugin.Payments.Nochex.Controllers" } 
      );
    }

    public int Priority {
      get { return 0; }
    }
  }
}
