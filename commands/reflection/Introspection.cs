using System.Diagnostics;
using System.Reflection;

namespace MoarUtils.Utils {
  public static class Introspection {
    public static string GetMethodAndClass() {
      MethodBase methodInfo = new StackFrame(1).GetMethod();
      string classAndMethod = methodInfo.DeclaringType.Name + "|" + methodInfo.Name;
      return classAndMethod;
    }
  }
}
