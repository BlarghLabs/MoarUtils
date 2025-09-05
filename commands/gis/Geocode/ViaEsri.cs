using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MoarUtils;
using MoarUtils.commands.logging;
using MoarUtils.enums;
using MoarUtils.models.commands;
using MoarUtils.models.gis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace moarutils.utils.gis.geocode {
  //http://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/find?text=1700 Penny ave washingtn dc&f=pjson&forStorage=false&maxLocations=1

  public static class ViaEsri {
    public class Request {
      public string address { get; set; }
      public string key { get; set; }
      //public int maxTriesIfQueryLimitReached { get; set; } = 1;
      //public bool useRateLimit { get; set; }
      //public bool throwOnUnableToGeocode { get; set; } = true;

    }
    public class Response : ResponseStatusModel {
      public Coordinate coordinate { get; set; }
    }
    public static async Task<Response> Execute(
      Request request,
      CancellationToken cancellationToken,
      WebProxy wp = null
    ) {
      var response = new Response { };

      try {
        request.address = request.address?.Trim();
        if (string.IsNullOrEmpty(request.address)) {
          return response = new Response { status = "address required" };
        }
        //note: ersi puts "0" in china, maybe ignore this one input?
        if (request.address.Equals("0")) {
          return response = new Response { status = "0 is not properly geocoded by this provider" };
        }
        if (request.address.Length > 200) {
          LogIt.W("address was > 200 length, truncating: " + request.address);
          request.address = request.address.Substring(0, 200);
        }

        var rgx = new Regex(@"[^\w\s]*");
        var addressCheck = rgx.Replace(request.address.Trim(), "").Trim();
        //maybe do replace on nont letter number?
        if (string.IsNullOrWhiteSpace(addressCheck)) {
          return response = new Response { status = "address had no numbers or letters: " + request.address.Trim() };
        }

        var uea = HttpUtility.UrlEncode(request.address.Trim());
        var resource = "/arcgis/rest/services/World/GeocodeServer/find?text=" + uea + "&f=pjson&forStorage=false&maxLocations=1";
        var client = new RestClient("https://geocode.arcgis.com/");
        var restRequest = new RestRequest(
          resource: resource,
          method: Method.Get
        );
        //if (wp != null) {
        //  client.Proxy = wp;
        //}
        var restResponse = await client.ExecuteAsync(restRequest);

        if (restResponse.ErrorException != null) {
          return response = new Response { status = $"response had error exception: {restResponse.ErrorException.Message}" };
        }
        if (restResponse.StatusCode != HttpStatusCode.OK) {
          return response = new Response { status = $"StatusCode was {restResponse.StatusCode}" };
        }
        if (string.IsNullOrWhiteSpace(restResponse.Content)) {
          return response = new Response { status = $"content was empty" };
        }
        var content = restResponse.Content;
        dynamic json = JObject.Parse(content);

        if ((json.locations == null) || (json.locations.Count == 0)) {
          //maybe look at: json.error.code.Value
          LogIt.W(content);
          return response = new Response { status = $"location result was null" };
        }

        var geometry = json.locations[0].feature.geometry;
        if (geometry != null) {
          var lng = Convert.ToDecimal(geometry.x.Value);
          var lat = Convert.ToDecimal(geometry.y.Value);
          if ((lat != 0) && (lng != 0)) {
            return response = new Response {
              httpStatusCode = HttpStatusCode.OK,
              coordinate = new Coordinate {
                geocoder = Geocoder.Esri,
                lng = lng,
                lat = lat,
                precision = (json.locations[0].feature.attributes == null)
                  ? null
                  : Convert.ToString(json.locations[0].feature.attributes.Score.Value)
                ,
              }
            };
          }
        }

        return response = new Response { status = "not found" };
      } catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          return response = new Response { status = Constants.ErrorMessages.CANCELLATION_REQUESTED_STATUS };
        }
        LogIt.E(ex);
        return response = new Response { status = Constants.ErrorMessages.UNEXPECTED_ERROR_STATUS, httpStatusCode = HttpStatusCode.InternalServerError };
      } finally {
        //dtLastRequest = DateTime.UtcNow;
        LogIt.I(JsonConvert.SerializeObject(new {
          response.httpStatusCode,
          response.status,
          request?.address,
          response?.coordinate,
        }, Formatting.Indented));
      }
    }
  }
}


#region using nuget
//public static async Task<Response> Execute(
//  out HttpStatusCode hsc,
//  out string status,
//  out Coordinate c,
//  string address,
//  string apiToken,
//  WebProxy wp = null
//) {
//  c = new Coordinate { g = Geocoder.Esri };
//  hsc = HttpStatusCode.BadRequest;
//  status = "";

//  try {
//    if (string.IsNullOrEmpty(address)) {
//      status = $"address required";
//      hsc = HttpStatusCode.BadRequest;
//      return;
//    }
//    //note: ersi puts "0" in china, maybe ignore this one input?
//    if (address.Equals("0")) {
//      status = $"0 is not properly geocoded by this provider";
//      hsc = HttpStatusCode.BadRequest;
//      return;
//    }

//    if (address.Length > 200) {
//      LogIt.W("address was > 200 length, truncating: " + address);
//      address = address.Substring(0, 200);
//    }

//    //var rgx = new Regex(@"[^\w\s]*");
//    //var addressCheck = rgx.Replace(address.Trim(), "").Trim();
//    //if (string.IsNullOrWhiteSpace(addressCheck)) {
//    //  status = $"address had no numbers or letters: " + address.Trim();
//    //  hsc = HttpStatusCode.BadRequest;
//    //  return;
//    //}

//    ////https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates?address={searchText}&outFields={fieldList}&f=json&token=<ACCESS_TOKEN>

//    //var uea = HttpUtility.UrlEncode(address.Trim());
//    //var resource = $"/arcgis/rest/services/World/GeocodeServer/findAddressCandidates?address={address}&outFields=*&f=json&token={apiToken}";
//    //var client = new RestClient("https://geocode-api.arcgis.com/");
//    //var request = new RestRequest(
//    //  resource: resource,
//    //  method: Method.Get
//    //);
//    ////if (wp != null) {
//    ////  client.Proxy = wp;
//    ////}
//    //var response = client.ExecuteAsync(request).Result;

//    //if (response.ErrorException != null) {
//    //  status = $"response had error exception: {response.ErrorException.Message}";
//    //  hsc = HttpStatusCode.BadRequest;
//    //  return;
//    //}
//    //if (response.StatusCode != HttpStatusCode.OK) {
//    //  status = $"StatusCode was {response.StatusCode}";
//    //  hsc = HttpStatusCode.BadRequest;
//    //  return;
//    //}
//    //if (string.IsNullOrWhiteSpace(response.Content)) {
//    //  status = $"content was empty";
//    //  hsc = HttpStatusCode.BadRequest;
//    //  return;
//    //}
//    //var content = response.Content;
//    //dynamic json = JObject.Parse(content);

//    //if ((json.locations == null) || (json.locations.Count == 0)) {
//    //  //maybe look at: json.error.code.Value
//    //  LogIt.W(content);
//    //  status = $"location result was null";
//    //  hsc = HttpStatusCode.OK;
//    //  return;
//    //}


//    //var geometry = json.locations[0].feature.geometry;
//    //if (geometry != null) {
//    //  var lng = Convert.ToDecimal(geometry.x.Value);
//    //  var lat = Convert.ToDecimal(geometry.y.Value);
//    //  if ((lat != 0) && (lng != 0)) {
//    //    c.lng = lng;
//    //    c.lat = lat;
//    //  }
//    //}
//    //var precision = json.locations[0].feature.attributes;
//    //if (precision != null) {
//    //  var pc = precision.Score.Value;
//    //  c.precision = Convert.ToString(pc);
//    //}

//    Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = apiToken;

//    var locatorTask = new LocatorTask(new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer"));
//    // Or set an APIKey on the Locator Task:
//    // locatorTask.ApiKey = "YOUR_API_KEY";

//    var results = locatorTask.GeocodeAsync(address).Result;
//    if (!results.Any()) {
//      status = $"no results";
//      hsc = HttpStatusCode.OK;
//      return;
//    }
//    LogIt.I(results.First().DisplayLocation);
//    c.lng = Convert.ToDecimal(results.First().DisplayLocation.X);
//    c.lat = Convert.ToDecimal(results.First().DisplayLocation.Y);
//    c.precision = results.First().Score.ToString();

//    //HERE HERE HERE
//    //if (results?.FirstOrDefault() is GeocodeResult firstResult) {
//    //  Console.WriteLine($"Found {firstResult.Label} at {firstResult.DisplayLocation} with score {firstResult.Score}");
//    //  firstResult.DisplayLocation.la
//    hsc = HttpStatusCode.OK;
//    return;
//    //}

//    //status = "no results (1)";
//    //hsc = HttpStatusCode.BadRequest;
//    //return;
//  } catch (Exception ex) {
//    status = $Constants.ErrorMessages.UNEXPECTED_ERROR_STATUS;
//    hsc = HttpStatusCode.InternalServerError;
//    LogIt.E(ex);
//  } finally {
//    LogIt.I(JsonConvert.SerializeObject(new {
//      hsc,
//      status,
//      address,
//      c,
//    }, Formatting.Indented));
//  }
//}
#endregion



