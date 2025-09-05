using System;
using System.Threading.Tasks;
using System.Web;
using MoarUtils.commands.logging;

namespace MoarUtils.commands.web {
  public class LogHttpHeaders {
    public static async Task Execute() {
      try {
        foreach (var rh in HttpContext.Current.Request.Headers) {
          LogIt.D(rh.ToString() + "|" + HttpContext.Current.Request.Headers[rh.ToString()]);
        }
      } catch (Exception ex) {
        LogIt.E(ex);
      }
    }
  }
}