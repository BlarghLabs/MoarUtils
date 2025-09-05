using System.Security.Principal;

namespace MoarUtils.commands.web {
  public class UnauthenticatedUser {
    public static bool Execute(
      IPrincipal ip
    ) {
      //ip?.Identity?.Name
      var result =
        (ip == null)
        || (ip.Identity == null)
        || !ip.Identity.IsAuthenticated
        || string.IsNullOrWhiteSpace(ip.Identity.Name);
      return result;
    }

  }
}
