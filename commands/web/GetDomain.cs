using System;

namespace MoarUtils.commands.web {
  public class GetDomain {
    public static string Execute(string url) {
      var domain = "";
      try {
        var uri = new Uri(url);
        domain = !string.IsNullOrWhiteSpace(uri.Host) ? uri.Host : (url.Contains(":") ? url.Substring(0, url.IndexOf(":")) : url);
      } catch { } finally {
        domain = string.IsNullOrWhiteSpace(domain) ? null : domain;
      }
      return domain;
    }
  }
}
