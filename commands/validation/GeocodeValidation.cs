namespace MoarUtils.Utils.Validation {
  public class GeocodeValidation {

    public static bool IsValid(decimal latitude, decimal longitude, bool treatDoubleZeroAsInvalid = true) {
      return IsValid((double)latitude, (double)longitude, treatDoubleZeroAsInvalid);
    }

    public static bool IsValid(double latitude, double longitude, bool treatDoubleZeroAsInvalid = true) {
      // Latitude must be between -90 and 90 degrees
      bool isValidLatitude = IsValidLatitude(latitude);

      // Longitude must be between -180 and 180 degrees
      bool isValidLongitude = IsValidLongitude(longitude);

      return
        isValidLatitude
        &&
        isValidLongitude
        &&
        (!treatDoubleZeroAsInvalid ? true : !(latitude == 0 && longitude == 0));
    }

    // Alternative: Separate validation methods
    public static bool IsValidLatitude(double latitude) {
      return latitude >= -90.0 && latitude <= 90.0;
    }

    public static bool IsValidLongitude(double longitude) {
      return longitude >= -180.0 && longitude <= 180.0;
    }
  }
}