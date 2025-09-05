using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MoarUtils;
using MoarUtils.commands.logging;
using MoarUtils.commands.strings;
using MoarUtils.enums;
using MoarUtils.models.commands;
using MoarUtils.models.gis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace moarutils.utils.gis.geocode {
  public static class ViaGoogle {

    public class Request {
      public string address { get; set; }
      public string key { get; set; }
      public int maxTriesIfQueryLimitReached { get; set; } = 1;
    }
    public class Response : ResponseStatusModel {
      public Coordinate coordinate { get; set; }
    }


    private const double m_dThrottleSeconds = .9; //1.725; //0.1; //100ms //Convert.ToDouble(1.725);
    private static DateTime dtLastRequest = DateTime.UtcNow;
    private static Mutex mLastRequest = new Mutex();

    private static string GetUrlSecondPart(string location, string key = null) {
      string locationUrl = "";
      if (!String.IsNullOrEmpty(location)) {
        locationUrl = "address=" + Uri.EscapeDataString(CondenseWhiteSpace.Execute(HttpUtility.UrlEncode(location.Replace("+", " "))));
      }
      return "maps/api/geocode/json?" + locationUrl + "&sensor=false" + (string.IsNullOrEmpty(key) ? "" : $"&key={key}");
    }

    public static async Task<Response> Execute(
      Request request,
      CancellationToken cancellationToken,
      WebProxy wp = null
    ) {
      lock (mLastRequest) {
        //Force delay of 1.725 seconds between requests: re: http://groups.google.com/group/Google-Maps-API/browse_thread/thread/906e871bcb8c15fd
        TimeSpan tsDuration;
        bool bRequiredWaitTimeHasElapsed;
        do {
          tsDuration = DateTime.UtcNow - dtLastRequest;
          bRequiredWaitTimeHasElapsed = (tsDuration.TotalSeconds > m_dThrottleSeconds);
          if (!bRequiredWaitTimeHasElapsed) {
            int iMillisecondsToSleep = Convert.ToInt32((m_dThrottleSeconds - tsDuration.TotalSeconds) * Convert.ToDouble(1000));
            Thread.Sleep(iMillisecondsToSleep);
          }
        } while (!bRequiredWaitTimeHasElapsed);
      }

      var response = await ExecuteNoRateLimit(
        request: request,
        wp: wp,
        cancellationToken: cancellationToken
      );
      if (response.httpStatusCode != HttpStatusCode.OK) {
        LogIt.E("unable to geocode");
      }
      return response;
    }

    public static async Task<Response> ExecuteNoRateLimit(
      Request request,
      CancellationToken cancellationToken,
      WebProxy wp = null
    ) {
      var response = new Response { };
      try {
        request.address = request?.address?.Trim();
        if (string.IsNullOrEmpty(request.address)) {
          return response = new Response { status = "address required" };
        }

        int trys = 1;
        do {
          var client = new RestClient("https://maps.googleapis.com/");
          var restRequest = new RestRequest(
            resource: GetUrlSecondPart(request.address, request.key),
            method: Method.Get
          //dataFormat: DataFormat.Json
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
          response.status = json.status.Value;
          switch (response.status) {
            case "OK":
              var results = json.results;
#if DEBUG
              Console.WriteLine(results);
#endif
              var lat = json.results[0].geometry.location.lat.ToString();
              lat = string.IsNullOrWhiteSpace(lat)
                ? ""
                : lat
              ;
              var lng = json.results[0].geometry.location.lng.ToString();
              lng = string.IsNullOrWhiteSpace(lng)
                ? ""
                : lng
              ;

              var location_type = json.results[0].geometry.location_type;
              location_type = (location_type == null)
                ? ""
                : location_type
              ;
              return response = new Response {
                httpStatusCode = HttpStatusCode.OK,
                coordinate = new Coordinate {
                  geocoder = Geocoder.Google,
                  lat = Convert.ToDecimal(lat),
                  lng = Convert.ToDecimal(lng),
                  precision = location_type
                }
              };
            case "UNKNOWN_ERROR":
              if (trys < request.maxTriesIfQueryLimitReached) {
                Thread.Sleep(1000 * trys);
              }
              break;
            default:
              //case "OVER_QUERY_LIMIT":
              //case "REQUEST_DENIED":
              //case "INVALID_REQUEST":
              //case "ZERO_RESULTS":
              return response = new Response {
                status = $"status was {response.status}",
              };
          }
          trys++;
        } while (trys < request.maxTriesIfQueryLimitReached);

        return response = new Response {
          status = $"unable to geocode",
        };
      } catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          return response = new Response { status = Constants.ErrorMessages.CANCELLATION_REQUESTED_STATUS };
        }
        LogIt.E(ex);
        return response = new Response { status = Constants.ErrorMessages.UNEXPECTED_ERROR_STATUS, httpStatusCode = HttpStatusCode.InternalServerError };
      } finally {
        dtLastRequest = DateTime.UtcNow;
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
