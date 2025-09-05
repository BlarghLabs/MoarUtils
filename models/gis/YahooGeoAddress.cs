namespace MoarUtils.models.gis {

  /// <summary>
  /// GeoAddress is an address object which includes latitude and longitude (and the precison of that geoCoding).
  /// </summary>
  public class YahooGeoAddress {
    public string street { get; set; }
    public string city { get; set; }
    public string stateCode { get; set; }
    public string zipCode { get; set; }
    public string latitude { get; set; }
    public string longitude { get; set; }
    public string precision { get; set; }
    public string warning { get; set; }
    public string errorMessage { get; set; }

    /*
    StringBuilder sbResult = new StringBuilder();
    sbResult.Append("<br/>Street:" + address.Street);
    sbResult.Append("<br/>City: " + address.City);
    sbResult.Append("<br/>State: " + address.StateCode);
    sbResult.Append("<br/>ZipCode: " + address.ZipCode);
    sbResult.Append("<br/>Latitude: " + address.Latitude);
    sbResult.Append("<br/>Longitude: " + address.Longitude);
    sbResult.Append("<br/>Precision: " + address.Precision);
    sbResult.Append("<br/>Warning: " + address.Warning);
    sbResult.Append("<br/>Error Message: " + address.ErrorMessage);
    */
  }
}