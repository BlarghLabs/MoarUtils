using System;
using System.Web;
using MoarUtils.commands.logging;

namespace MoarUtils.commands.web {
  public class GetUserAgent {
    /// <summary>
    /// account for possbility of ELB sheilding the public ip
    /// </summary>
    /// <returns></returns>
    public static string Execute(bool elbIsInUse = true) {
      try {
#if DEBUG
#else
#endif
        if ((HttpContext.Current == null) || (HttpContext.Current.Request == null)) {
          return null;
        }
        var value = HttpContext.Current.Request.Headers["User-Agent"];
        return value;
      } catch (Exception ex) {
        LogIt.E(ex);
      }
      return null;
    }
  }


}


