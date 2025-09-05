using MoarUtils.enums;


namespace MoarUtils.models.gis {
  public class Coordinate {
    public decimal lat;
    public decimal lng;
    public string precision;

    public string title;
    public string desc;
    public string iconUrl;

    public Geocoder geocoder = Geocoder.Unknown;


    public string ToString(bool bMakeGoogleMapLink, string sTargetName) {
      string sResult = "";
      try {
        sResult = "Lat=" + lat.ToString() + "|Long=" + lng.ToString() + "|Src=" + geocoder.ToString() + "|Precision=" + precision;

        if (bMakeGoogleMapLink) {
          sTargetName = string.IsNullOrWhiteSpace(sTargetName) ? "_gmaplink" : sTargetName;
          sResult = "<a target='" + sTargetName + "' href='http://maps.google.com/maps?q=" + lat.ToString() + "," + lng.ToString() + "'>" + sResult + "</a>";

          if (geocoder == Geocoder.Yahoo) {
            sTargetName = string.IsNullOrWhiteSpace(sTargetName) ? "_gmaplink" : sTargetName;
            sResult += "|<a target='" + sTargetName + "' href='http://maps.yahoo.com/#mvt=request&lat=" + lat.ToString() + "&lon=" + lng.ToString() + "&tp=1&zoom=14'>Y</a>";
          }
          /*
          if (_eGeocodeSource == GeocodeSource.MapQuest) {
            sTargetName = string.IsNullOrWhiteSpace(sTargetName) ? "_gmaplink" : sTargetName;
            sResult += "|<a target='" + sTargetName + "' href='http://maps.yahoo.com/#mvt=request&lat=" + _latitude.ToString() + "&lon=" + _longitude.ToString() + "&tp=1&zoom=14'>MQ</a>";
          } 
          */
        }
      } catch /* (Exception ex) */ { }

      return sResult;
    }
  }
}