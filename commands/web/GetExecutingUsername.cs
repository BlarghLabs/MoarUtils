using System;
using System.Threading;
using MoarUtils.commands.logging;
namespace MoarUtils.commands.web {
  public class GetExecutingUsername {
    public static string Execute() {
      string executedBy = null;
      try {
        var ip = Thread.CurrentPrincipal;
        executedBy = (ip == null || ip.Identity == null || !ip.Identity.IsAuthenticated)
          ? null
          : ip.Identity.Name;
      } catch (Exception ex) {
        LogIt.E(ex);
      }
      return executedBy;
    }
  }


}