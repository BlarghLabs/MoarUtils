using System;
using System.Linq;
using System.Text;

namespace MoarUtils.Utils {
  public class LinqUtils {
    public static string ToTraceString<T>(IQueryable<T> query) {
      var internalQueryField = query.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(f => f.Name.Equals("_internalQuery")).FirstOrDefault();

      var internalQuery = internalQueryField.GetValue(query);

      var objectQueryField = internalQuery.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(f => f.Name.Equals("_objectQuery")).FirstOrDefault();

      var objectQuery = objectQueryField.GetValue(internalQuery) as System.Data.Entity.Core.Objects.ObjectQuery<T>;

      return ToTraceStringWithParameters<T>(objectQuery);
    }


    public static string ToTraceStringWithParameters<T>(System.Data.Entity.Core.Objects.ObjectQuery<T> query) {
      System.Text.StringBuilder sb = new StringBuilder();

      string traceString = query.ToTraceString() + Environment.NewLine;

      foreach (var parameter in query.Parameters) {
        traceString += parameter.Name + " [" + parameter.ParameterType.FullName + "] = " + parameter.Value + "\n";
      }

      return traceString;
    }

  }
}
