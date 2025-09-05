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
  //curl "https://api.geocod.io/v1.4/geocode?q=1109+N+Highland+St%2c+Arlington+VA&api_key=YOUR_API_KEY"

  public static class ViaGeocodio {
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
        request.address = request?.address?.Trim();
        request.key = request?.key?.Trim();
        if (string.IsNullOrEmpty(request.address)) {
          return response = new Response { status = "address required" };
        }
        //note: ersi puts "0" in china, maybe ignore this one input?
        if (request.address.Equals("0")) {
          return response = new Response { status = "0 is not properly geocoded by this provider" };
        }

        //if (address.Length > 200) {
        //  LogIt.W("address was > 200 length, truncating: " + address);
        //  address = address.Substring(0, 200);
        //}

        var rgx = new Regex(@"[^\w\s]*");
        var addressCheck = rgx.Replace(request.address.Trim(), "").Trim();
        if (string.IsNullOrWhiteSpace(addressCheck)) {
          return response = new Response { status = "address had no numbers or letters: " + request.address.Trim() };
        }

        var uea = HttpUtility.UrlEncode(request.address.Trim());
        //geocode?q=1109+N+Highland+St%2c+Arlington+VA&api_key=YOUR_API_KEY"
        var resource = $"geocode?api_key={request.key}&limit=1&q=" + uea; // + "&f=pjson&forStorage=false&maxLocations=1";
        var client = new RestClient("https://api.geocod.io/v1.4/");
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
          return response = new Response { status = "content was empty" };
        }
        var content = restResponse.Content;
        dynamic json = JObject.Parse(content);

        if ((json.results == null) || (json.results.Count == 0)) {
          //maybe look at: json.error.code.Value
          LogIt.W(content);
          return response = new Response { status = $"no results found" };
        }

        var location = json.results[0].location;
        if (location != null) {
          var lng = Convert.ToDecimal(location.lat.Value);
          var lat = Convert.ToDecimal(location.lng.Value);
          if ((lat != 0) && (lng != 0)) {

            if (response.coordinate.lat == 0 || response.coordinate.lng == 0) {
              LogIt.W("here");
            }

            return response = new Response {
              coordinate = new Coordinate {
                geocoder = Geocoder.Geocodio,
                lng = lng,
                lat = lat,
                precision = json.results[0].accuracy_type == null ? null : json.results[0].accuracy_type.Value
              },
              httpStatusCode = HttpStatusCode.OK
            };
          }
        }

        return response = new Response { status = "unable to parse lat/lng from response" };
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
