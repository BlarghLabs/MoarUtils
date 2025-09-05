using System;

namespace MoarUtils.Utils {
  public static class EnumUtils {
    public static bool TryParseEnum<TEnum>(this int enumValue, out TEnum retVal) {
      retVal = default(TEnum);
      bool success = Enum.IsDefined(typeof(TEnum), enumValue);
      if (success) {
        retVal = (TEnum)Enum.ToObject(typeof(TEnum), enumValue);
      }
      return success;
    }

    public static T ParseEnum<T>(string value) {
      return (T)Enum.Parse(typeof(T), value, true);
    }

    //public static T ToEnum<T>(this string value, T defaultValue) {
    //  if (string.IsNullOrWhiteSpace(value)) {
    //    return defaultValue;
    //  }

    //  T result;
    //  return Enum.TryParse<T>(value, true, out result) ? result : defaultValue;
    //}
  }
}
