using System;
using System.Net;
using System.Web;
using MoarUtils.commands.logging;
using MoarUtils.commands.strings;
using MoarUtils.enums;
using MoarUtils.models.gis;
using RestSharp;


//http://developer.yahoo.com/boss/geo/docs/free_YQL.html
//http://developer.yahoo.com/yql/console/?q=select%20*%20from%20geo.placefinder%20where%20text%3D%22sfo%22#h=select%20*%20from%20geo.placefinder%20where%20text%3D%22washington%20dc%22
//http://gws2.maps.yahoo.com/findlocation?pf=1&locale=en_US&flags=&offset=15&gflags=&q=lake%20claire%20ct%20atl&start=0&count=100
//http://gws2.maps.yahoo.com/findlocation?pf=1&locale=en_US&flags=&offset=15&gflags=&q=lake%20claire%20ct%20atl&start=0&count=1
//http://gws2.maps.yahoo.com/findlocation?pf=1&locale=en_US&flags=&offset=15&gflags=&q=foo&start=0&count=1

//xml
//https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20geo.placefinder%20where%20text%3D%22washington%20dc%22&diagnostics=true
//json
//https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20geo.placefinder%20where%20text%3D%22washington%20dc%22&format=json&diagnostics=true&callback=
//https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20geo.placefinder%20where%20text%3D%22washington%20dc%22&format=json

namespace moarutils.utils.gis.geocode {
  public class ViaYahoo {
    public static Coordinate Execute(string location, WebProxy wp = null) {
      var c = new Coordinate { geocoder = Geocoder.Yahoo };
      if (!string.IsNullOrWhiteSpace(location)) {
        c = QueryYahoo(location, wp);
      }
      return c;
    }


    private static string GetYQLUrl(string location) {
      //new
      //https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20geo.placefinder%20where%20text%3D%22washington%20dc%22&format=json
      return "https://query.yahooapis.com/" + GetYQLUrlSecondPart(location);
    }

    private static string GetYQLUrlSecondPart(string location) {
      //https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20geo.placefinder%20where%20text%3D%2220009%22&format=json
      //https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20geo.placefinder%20where%20text%3D%22LOCATIONHERE%22&format=json

      // Build sUrl request to be sent to Yahoo!
      string locationUrl = "";
      if (!String.IsNullOrWhiteSpace(location)) {
        locationUrl = "v1/public/yql?q=select%20*%20from%20geo.placefinder%20where%20text%3D%22" + CondenseWhiteSpace.Execute(HttpUtility.UrlEncode(location.Replace("+", " "))) + "%22&format=json";
      }
      return locationUrl;
    }

    //TODO: allow passig vairable if retries is dsired
    private static Coordinate QueryYahoo(string location, WebProxy wp = null /*, out bool error */) {
      try {
        //Cert.IgnoreInvalidCerts();

        var client = new RestClient("https://query.yahooapis.com/");
        //if (wp != null) {
        //  client.Proxy = wp;
        //}
        var request = new RestRequest(GetYQLUrlSecondPart(location), Method.Get);
        request.RequestFormat = DataFormat.Json;
        var response = client.ExecuteAsync(request).Result;

        #region try again if response code not valid or exception found
        int attempts = 0;
        while ((response.ErrorException != null) || (response.StatusCode != HttpStatusCode.OK)) {
          if (response.ErrorException != null) {
            LogIt.W(response.ErrorException.Message);
          } else {
            LogIt.W(response.StatusCode.ToString());
          }
          //try again
          response = client.ExecuteAsync(request).Result;
          attempts++;
          if ((attempts > 2) && ((response.ErrorException != null) || (response.StatusCode != HttpStatusCode.OK))) {
            if (response.ErrorException != null) {
              throw response.ErrorException;
            } else {
              throw new Exception(response.StatusCode.ToString());
            }
          }
        }
        #endregion

        var content = response.Content;
        dynamic json = Newtonsoft.Json.Linq.JObject.Parse(content);

        //what to do if more than one result?
        if (json.query.count.Value < 1) {
          return new Coordinate { geocoder = Geocoder.Yahoo, precision = "NO RESULTS" };
        } else {

          //check for error messages?
          var yr = new YahooApiGeocoderResult {
            quality = (json.query.count.Value > 1) ? json.query.results.Result[0].quality.Value : json.query.results.Result.quality.Value,
            latitude = (json.query.count.Value > 1) ? json.query.results.Result[0].latitude.Value : json.query.results.Result.latitude.Value,
            longitude = (json.query.count.Value > 1) ? json.query.results.Result[0].longitude.Value : json.query.results.Result.longitude.Value
          };

          //convert it
          var c = new Coordinate {
            geocoder = Geocoder.Yahoo,
            lat = Decimal.Parse(yr.latitude, System.Globalization.NumberStyles.Float),
            lng = Decimal.Parse(yr.longitude, System.Globalization.NumberStyles.Float),
            precision = yr.quality
          };
          return c;
        }
      } catch (Exception ex) {
        //error = true;
        LogIt.W(location);
        LogIt.E(ex);
        throw ex;
      }

    }
  }
}
