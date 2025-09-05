using System.Web.Mvc;

namespace MoarUtils.filters {
  public class XFrameOptionAllowAll : ActionFilterAttribute {
    public override void OnActionExecuting(ActionExecutingContext filterContext) {
      filterContext.HttpContext.Response.Headers.Remove("X-Frame-Options");
      filterContext.HttpContext.Response.AddHeader("X-Frame-Options", "AllowAll");

      base.OnActionExecuting(filterContext);
    }
  }
}

