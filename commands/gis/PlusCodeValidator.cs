using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoarUtils.commands.gis {
  public static class PlusCodeValidator {
    // Valid characters for Plus Codes (Base 20 encoding)
    private static readonly string ValidChars = "23456789CFGHJMPQRVWX";

    /// <summary>
    /// LINQ-safe format detection - checks basic Plus Code pattern without exceptions
    /// Safe to use in LINQ queries, won't throw on invalid input
    /// </summary>
    /// <param name="code">The code to check</param>
    /// <returns>True if format appears to be a Plus Code pattern</returns>
    public static bool IsLikelyPlusCodeFormat(string code) {
      // Null/empty check - safe for LINQ
      if (string.IsNullOrWhiteSpace(code))
        return false;

      try {
        code = code.Trim().ToUpper();

        // Must contain exactly one '+' 
        int plusIndex = code.IndexOf('+');
        if (plusIndex <= 0 || code.LastIndexOf('+') != plusIndex)
          return false;

        // Basic length check
        if (code.Length < 3 || code.Length > 12) // minimum: "XX+" maximum: "XXXXXXXX+XXX"
          return false;

        // Split safely
        string beforePlus = code.Substring(0, plusIndex);
        string afterPlus = code.Substring(plusIndex + 1);

        // Length constraints
        if (beforePlus.Length < 2 || beforePlus.Length > 8 || beforePlus.Length % 2 != 0)
          return false;

        if (afterPlus.Length > 3)
          return false;

        // Character validation using LINQ (safe operations)
        bool validMainChars = beforePlus.All(c => ValidChars.Contains(c));
        bool validRefinementChars = afterPlus.All(c => ValidChars.Contains(c));

        return validMainChars && validRefinementChars;
      } catch {
        // Any exception means it's not a valid format
        return false;
      }
    }


    /// <summary>
    /// Determines if a string is in valid Plus Code format
    /// </summary>
    /// <param name="code">The code to validate</param>
    /// <returns>True if valid Plus Code format, false otherwise</returns>
    public static bool IsValidPlusCode(string code) {
      if (string.IsNullOrWhiteSpace(code))
        return false;

      // Remove any whitespace
      code = code.Trim().ToUpper();

      // Must contain exactly one '+' character
      int plusIndex = code.IndexOf('+');
      if (plusIndex == -1 || code.LastIndexOf('+') != plusIndex)
        return false;

      // Split into main code and refinement
      string mainCode = code.Substring(0, plusIndex);
      string refinement = code.Substring(plusIndex + 1);

      // Validate main code length (must be even, between 2-8 characters)
      if (mainCode.Length < 2 || mainCode.Length > 8 || mainCode.Length % 2 != 0)
        return false;

      // Validate refinement length (0, 1, 2, or 3 characters)
      if (refinement.Length > 3)
        return false;

      // Check if all characters are valid
      foreach (char c in mainCode) {
        if (!ValidChars.Contains(c))
          return false;
      }

      foreach (char c in refinement) {
        if (!ValidChars.Contains(c))
          return false;
      }

      // Additional validation: first character cannot be '0' or '1' 
      // (not in valid character set anyway, but good to be explicit)

      // Validate coordinate ranges (basic check)
      if (!IsValidCoordinateRange(mainCode))
        return false;

      return true;
    }

    /// <summary>
    /// Checks if the main code represents valid coordinate ranges
    /// </summary>
    private static bool IsValidCoordinateRange(string mainCode) {
      // This is a simplified validation - full validation would require
      // decoding the entire coordinate system

      // The first two characters represent the latitude band
      // Valid latitude codes should not exceed certain values
      if (mainCode.Length >= 2) {
        char firstChar = mainCode[0];
        char secondChar = mainCode[1];

        // Basic range check for latitude (rough approximation)
        // Full implementation would decode the base-20 values
        int firstValue = ValidChars.IndexOf(firstChar);
        int secondValue = ValidChars.IndexOf(secondChar);

        // Very basic sanity check - this could be more sophisticated
        if (firstValue < 0 || secondValue < 0)
          return false;
      }

      return true;
    }

    /// <summary>
    /// Determines if a string matches Plus Code format using regex
    /// (Alternative simpler approach for format checking only)
    /// </summary>
    /// <param name="code">The code to check</param>
    /// <returns>True if format matches Plus Code pattern</returns>
    public static bool IsValidPlusCodeFormat(string code) {
      if (string.IsNullOrWhiteSpace(code))
        return false;

      // Regex pattern for Plus Code format
      // [ValidChars]{2,8}+[ValidChars]{0,3}
      string pattern = @"^[23456789CFGHJMPQRVWX]{2,8}\+[23456789CFGHJMPQRVWX]{0,3}$";

      return Regex.IsMatch(code.Trim().ToUpper(), pattern);
    }

    /// <summary>
    /// Gets the precision level of a Plus Code in meters
    /// </summary>
    /// <param name="code">Valid Plus Code</param>
    /// <returns>Approximate precision in meters, or -1 if invalid</returns>
    public static double GetPrecisionInMeters(string code) {
      if (!IsValidPlusCodeFormat(code))
        return -1;

      code = code.Trim().ToUpper();
      int plusIndex = code.IndexOf('+');

      string mainCode = code.Substring(0, plusIndex);
      string refinement = code.Substring(plusIndex + 1);

      // Calculate precision based on code length
      int totalLength = mainCode.Length + refinement.Length;

      switch (totalLength) {
        case 2:
          return 2500000;  // ~2500km
        case 4:
          return 125000;   // ~125km
        case 6:
          return 6250;     // ~6.25km
        case 8:
          return 312.5;    // ~312m
        case 9:
          return 62.5;     // ~62m
        case 10:
          return 12.5;     // ~12.5m
        case 11:
          return 2.5;      // ~2.5m
        default:
          return 312.5;    // Default to 8-character precision
      }
    }
  }

  // Example usage class
  public class Program {
    public static void Main() {
      // Test cases
      string[] testCodes = {
            "8G4QVJ6W+P4",     // Valid
            "8FVC2222+22",      // Valid
            "CFX3+XF",          // Valid short code
            "invalid+code",     // Invalid characters
            "8G4QVJ6W",         // Missing +
            "8G4QVJ6W+P45",     // Too long refinement
            "",                 // Empty
            "8G4QVJ6W+P4X"      // Too long refinement
        };

      foreach (string code in testCodes) {
        bool isValid = PlusCodeValidator.IsValidPlusCode(code);
        bool formatMatch = PlusCodeValidator.IsValidPlusCodeFormat(code);
        double precision = PlusCodeValidator.GetPrecisionInMeters(code);

        Console.WriteLine($"Code: '{code}'");
        Console.WriteLine($"  Valid: {isValid}");
        Console.WriteLine($"  Format Match: {formatMatch}");
        Console.WriteLine($"  Precision: {precision}m");
        Console.WriteLine();
      }
    }
  }
}