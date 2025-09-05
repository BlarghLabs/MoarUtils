using System;

namespace MoarUtils.Utils.random {
  public class RandomStrings {
    private static Random r = new Random();
    public static string GetRandomLetter() {
      // This method returns a random lowercase letter.
      // ... Between 'a' and 'z' inclusize.
      int num = r.Next(0, 26); // Zero to 25
      char let = (char)('a' + num);
      return let.ToString();
    }

    public static string GetRandomString(int len = 5) {
      var word = "";
      for (int i = 0; i < len; i++) {
        word += GetRandomLetter();
      }
      return word;
    }
  }
}
