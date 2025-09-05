using System;
using System.Web;
using MoarUtils.commands.logging;
using Newtonsoft.Json;

namespace MoarUtils.commands.web {
  public class GetRawUrl {
    /// <summary>
    /// account for possbility of ELB sheilding the public ip
    /// </summary>
    /// <returns></returns>
    public static string Execute(
      HttpContext hc = null,
      bool log = false
    ) {
      var rawUrl = "";
      try {
        if ((hc == null) || (hc.Request == null)) {
          hc = HttpContext.Current;
        }
        if ((hc == null) || (hc.Request == null)) {
          return null;
        }

        rawUrl = hc.Request.RawUrl;
        return rawUrl;
      } catch (Exception ex) {
        LogIt.E(ex);
      } finally {
        if (log) {
          LogIt.D(JsonConvert.SerializeObject(new {
            rawUrl,
          }, Formatting.Indented));
        }
      }
      return null;
    }
  }
}
