using System;
using System.Net;
using MoarUtils.commands.logging;

namespace MoarUtils.commands.validation.ipaddresses {
  public class IsValidIpAddress {
    public static bool Execute(string input) {
      try {
        IPAddress address;
        if (IPAddress.TryParse(input, out address)) {
          switch (address.AddressFamily) {
            case System.Net.Sockets.AddressFamily.InterNetwork:
              // we have IPv4
              return true;
            case System.Net.Sockets.AddressFamily.InterNetworkV6:
              // we have IPv6
              return true;
            default:
              // umm... yeah... I'request going to need to take your red packet and...
              return false;
          }
        }
        return false;
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }

    }
  }
}