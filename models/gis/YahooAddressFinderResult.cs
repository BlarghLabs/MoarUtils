

namespace MoarUtils.models.gis {
  public class YahooAddressFinderResult {

    public int Error = -1; //0
    public string ErrorMessage = ""; //No error
    public string Locale = ""; //us_US
    public int Quality = 0; //60
    public int Found = -1; //1
    //Result
    public int quality = 0; //60
    public decimal latitude = 0; //38.919129
    public decimal longitude = 0; //-77.037698
    public decimal offsetlat = 0; //38.919129
    public decimal offsetlon = 0; //-77.037698
    public int radius = 0; //1400
    public string name = "";
    public string line1 = "";
    public string line2 = "";
    public string line3 = "";
    public string line4 = ""; //United States
    public string house = "";
    public string street = "";
    public string xstreet = "";
    public string unittype = "";
    public string unit = "";
    public string postal = ""; //20009
    public string neighborhood = "";
    public string city = ""; //Washington
    public string county = ""; //District of Columbia
    public string state = ""; //District of Columbia
    public string country = ""; //United States
    public string countrycode = ""; //US
    public string statecode = ""; //DC
    public string countycode = ""; //DC
    public string uzip = ""; //20009
    public string hash = "";
    public string woeid = ""; //12765846
    public string woetype = ""; //11

    //old
    //public YahooGeocodeResultPrecision eYahooGeocodeResultPrecision = YahooGeocodeResultPrecision.unset;
    //string latitude = "";
    //string longitude = "";
    //string precison = "";
    //string warning = "";
    //string errorMessage = "";
    //string sMessage = "";
  }
}