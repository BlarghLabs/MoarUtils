using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.Validation {
  public class PhoneNumberValidation {
    public static ValidationResult ValidateUsAndCanada(string number) {
      if (IsValidNorthAmericanNumber(number)) {
        return ValidationResult.Success;
      } else {
        return new ValidationResult(errorMessage);
      }
    }

    public const string errorMessage = "invalid phone number format";
    public const string errorMessageExtended = "invalid phone number format (only US and Canada are supported)";

    public static string StripNonNumericCharacters(string number) {
      string numbersOnly = "";
      for (int i = 0; i < number.Length; i++) {
        numbersOnly += Char.IsNumber(number[i]) ? number[i].ToString() : "";
      }
      return numbersOnly;
    }

    public static bool IsValidNorthAmericanNumber(string number) {
      bool isValid = false;
      try {
        string numbersOnly = StripNonNumericCharacters(number);

        //dnw: 2402164320
        //Regex regexObj = new Regex(@"(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]‌​)\s*)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)([2-9]1[02-9]‌​|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})");
        //http://www.asiteaboutnothing.net/regex/regex-quickstart.html
        Regex regexObj = new Regex(@"^1?([0-9]{3})([0-9]{3})([0-9]{4})$");
        if (regexObj.IsMatch(numbersOnly)) {
          string formattedPhoneNumber = regexObj.Replace(numbersOnly, "($1) $2-$3");
          isValid = true;
        } else {
          // Invalid phone number
        }
      } catch (Exception ex) {
        LogIt.E(ex);
      }
      return isValid;
    }
  }
}