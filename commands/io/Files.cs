using System.IO;
using System.Linq;

namespace MoarUtils.Utils.IO {

  public class Files {
    public static int GetFileLines(string path) {
      var lineCount = 0;
      using (var reader = File.OpenText(path)) {
        while (reader.ReadLine() != null) {
          lineCount++;
        }
      }
      return lineCount;
    }

    //http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
    public static string ExtractValidFileName(string name) {
      string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
      string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

      return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }

    public static string ExtractValidFileName2(string fileName, string replaceEmpty = "") {
      return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), replaceEmpty));
    }

  }
}
