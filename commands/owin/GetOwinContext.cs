using System.Net.Http;
using System.Web;
using Microsoft.Owin;

namespace MoarUtils.Utils.measurements {
  public static class GetOwinContext {
    public static IOwinContext Execute(this HttpRequestMessage request) {
      var context = request.Properties["MS_HttpContext"] as HttpContextWrapper;
      if (context != null) {
        return HttpContextBaseExtensions.GetOwinContext(context.Request);
      }
      return null;
    }
  }
}