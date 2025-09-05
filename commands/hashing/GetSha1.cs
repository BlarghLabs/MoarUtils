using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MoarUtils.commands.logging;

namespace MoarUtils.commands.hashing {
  public class GetSha1 {
    public static string ExecuteFile(string path) {
      try {
        using (FileStream fs = new FileStream(path, FileMode.Open)) {
          return Execute(fs);
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        throw ex;
      }
    }

    public static string Execute(Stream s) {
      try {
        using (var bs = new BufferedStream(s)) {
          using (var sha1 = new SHA1Managed()) {
            byte[] hash = sha1.ComputeHash(bs);
            var formatted = new StringBuilder(2 * hash.Length);
            foreach (var b in hash) {
              formatted.AppendFormat("{0:X2}", b);
            }
            var sha1Hash = formatted.ToString();
            return sha1Hash;
          }
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        throw ex;
      }
    }

    public static string Execute(byte[] ba) {
      try {
        using (var sha1 = new SHA1Managed()) {
          byte[] hash = sha1.ComputeHash(ba);
          var formatted = new StringBuilder(2 * hash.Length);
          foreach (var b in hash) {
            formatted.AppendFormat("{0:X2}", b);
          }
          var sha1Hash = formatted.ToString();
          return sha1Hash;
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        throw ex;
      }
    }


    public static string Execute(string input) {
      using (var sha1 = new SHA1Managed()) {
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(hash.Length * 2);

        foreach (byte b in hash) {
          // can be "x2" if you want lowercase
          sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
      }
    }
  }
}

//0C2E99D0949684278C30B9369B82638E1CEAD415
//0123456789012345678901234567890123456789