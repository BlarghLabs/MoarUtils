using System.Web.Mvc;

namespace MoarUtils.filters {
  public class AllowCrossSiteJsonAttribute : ActionFilterAttribute {
    public override void OnActionExecuting(ActionExecutingContext filterContext) {
      //why did we add this?
      filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
      base.OnActionExecuting(filterContext);
    }
  }
}
