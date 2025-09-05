using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoarUtils.commands.logging;

namespace MoarUtils.commands.csv {
  public class CreateFile {
    public static string Execute<T>(List<T> data, bool addHeaderRow = false) {
      try {
        var properties = typeof(T).GetProperties();
        var result = new StringBuilder();

        if (addHeaderRow) {
          var methods = properties
                      .Select(p => p.GetMethod)
                      .Select(v => StringToCSVCell(
                        ((v == null) ? "" : v.ToString())
                      ))
                      .Select(request => request.Substring(request.IndexOf("get_") + 4).Replace("()", ""));
          result.AppendLine(string.Join(",", methods));
        }

        foreach (var row in data) {
          var values = properties.Select(p => p.GetValue(row, null))
                                 .Select(v => StringToCSVCell(
                                  ((v == null) ? "" : v.ToString())
                                  ));
          var line = string.Join(",", values);
          result.AppendLine(line);
        }

        return result.ToString();
      } catch (Exception ex) {
        LogIt.E(ex);
        throw ex;
      }
    }

    private static string StringToCSVCell(string str) {
      try {
        bool mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
        if (mustQuote) {
          var sb = new StringBuilder();
          sb.Append("\"");
          foreach (char nextChar in str) {
            sb.Append(nextChar);
            if (nextChar == '"')
              sb.Append("\"");
          }
          sb.Append("\"");
          return sb.ToString();
        }

        return str;
      } catch (Exception ex) {
        LogIt.E(ex);
        throw ex;
      }
    }

  }
}