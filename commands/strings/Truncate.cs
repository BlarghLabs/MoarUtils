namespace MoarUtils.commands.strings {
  public class Truncate {
    public static string Execute(string value, int maxChars) {
      return value.Length <= maxChars ? value : value.Substring(0, maxChars) + " ..";
    }
  }
}
