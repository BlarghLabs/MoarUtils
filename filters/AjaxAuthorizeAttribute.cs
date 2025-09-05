using System.Net;
using System.Web.Mvc;

namespace MoarUtils.filters {
  public class AjaxAuthorizeAttribute : AuthorizeAttribute {
    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext) {
      var httpContext = filterContext.HttpContext;
      var request = httpContext.Request;
      var response = httpContext.Response;
      var user = httpContext.User;

      if (request.IsAjaxRequest()) {
        if (user.Identity.IsAuthenticated == false)
          response.StatusCode = (int)HttpStatusCode.Unauthorized;
        else
          response.StatusCode = (int)HttpStatusCode.Forbidden;

        response.SuppressFormsAuthenticationRedirect = true;
        response.End();
      }

      base.HandleUnauthorizedRequest(filterContext);
    }

    //protected override void HandleUnauthorizedRequest(AuthorizationContext context) {
    //  if (context.HttpContext.Request.IsAjaxRequest()) {
    //    var urlHelper = new UrlHelper(context.RequestContext);
    //    context.HttpContext.Response.StatusCode = 403;
    //    context.Result = new JsonResult {
    //      Data = new {
    //        Error = "NotAuthorized",
    //        LogOnUrl = urlHelper.Action("LogOn", "Account")
    //      },
    //      JsonRequestBehavior = JsonRequestBehavior.AllowGet
    //    };
    //  } else {
    //    base.HandleUnauthorizedRequest(context);
    //  }
    //}
  }
}
