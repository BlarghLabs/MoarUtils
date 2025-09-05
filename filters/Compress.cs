using System.IO.Compression;
using System.Web.Mvc;

namespace MoarUtils.filters {

  public class Compress : ActionFilterAttribute {
    public override void OnActionExecuting(ActionExecutingContext filterContext) {
      //http://stackoverflow.com/questions/15067049/asp-net-mvc-response-filter-is-null-when-using-actionfilterattribute-in-regist
      if (filterContext.IsChildAction) return;

      var encodingsAccepted = filterContext.HttpContext.Request.Headers["Accept-Encoding"];
      if (string.IsNullOrWhiteSpace(encodingsAccepted)) return;

      encodingsAccepted = encodingsAccepted.ToLowerInvariant();
      var response = filterContext.HttpContext.Response;

      if (encodingsAccepted.Contains("deflate")) {
        response.AppendHeader("Content-encoding", "deflate");
        response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
      } else if (encodingsAccepted.Contains("gzip")) {
        response.AppendHeader("Content-encoding", "gzip");
        response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
      }
    }
  }
}