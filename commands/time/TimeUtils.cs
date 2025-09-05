using System;

namespace MoarUtils.commands.time {
  public class GetDisplayTime {
    public static string Execute(int seconds) {
      var s =
        ((TimeSpan.FromSeconds((int)seconds).Hours == 0) ? "" : (TimeSpan.FromSeconds((int)seconds).Hours + "h "))
        + TimeSpan.FromSeconds((int)seconds).Minutes + "request "
        + TimeSpan.FromSeconds((int)seconds).Seconds + "s";
      return s;
    }
  }
}
