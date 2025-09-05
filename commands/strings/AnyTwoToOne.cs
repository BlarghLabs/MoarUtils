using System.Text.RegularExpressions;

namespace MoarUtils.commands.strings {
  public class AnyTwoToOne {
    public static string Execute(string orig) {
      RegexOptions options = RegexOptions.None;
      Regex regex = new Regex(@"[ ]{2,}", options);
      return regex.Replace(orig, @" ").Trim();
    }
  }
}