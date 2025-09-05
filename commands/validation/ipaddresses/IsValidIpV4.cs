using System;
using System.Linq;

namespace MoarUtils.commands.validation.ipaddresses {
  public class IsValidIpV4 {
    public bool Execute(string ip) {
      if (String.IsNullOrWhiteSpace(ip)) {
        return false;
      }

      string[] splitValues = ip.Split('.');
      if (splitValues.Length != 4) {
        return false;
      }

      byte tempForParsing;

      return splitValues.All(r => byte.TryParse(r, out tempForParsing));
    }
  }
}