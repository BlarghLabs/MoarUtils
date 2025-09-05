using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MoarUtils;
using MoarUtils.commands.gis;
using MoarUtils.commands.logging;
using MoarUtils.commands.strings;
using MoarUtils.models.commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace moarutils.utils.gis.geocode {
  public static class ViaGeoapify {

    public class Request {
      public string address { get; set; }
      public string key { get; set; }
    }
    public class Response : ResponseStatusModel {
      public double lat { get; set; }
      public double lng { get; set; }
      public string streetAddress { get; set; } //1 and 2 combined by comma
      //public string streetNumber { get; set; }
      //public string route { get; set; }
      public string countryCode { get; set; }
      public string city { get; set; }
      public string province { get; set; }
      //public string county { get; set; }
      public string postalCode { get; set; }
      public string normalizedFormattedAddress { get; set; }
      public string resultType { get; set; }
      public string category { get; set; }
      public string plusCode { get; set; }
      public string rankMatchType { get; set; }
      public string jsonResult { get; set; }

      //public Coordinate coordinate { get; set; }
    }

    private const double m_dThrottleSeconds = 1;
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
        #region validation
        if (request == null) {
          return response = new Response { status = "params were null" };
        }
        request.address = request?.address?.Trim();
        if (string.IsNullOrWhiteSpace(request.address)) {
          return response = new Response { status = "address was empty" };
        }
        if (string.IsNullOrWhiteSpace(request.key)) {
          return response = new Response { status = "key required" };
        }
        if (cancellationToken.IsCancellationRequested) {
          return response = new Response { status = Constants.ErrorMessages.CANCELLATION_REQUESTED_STATUS };
        }
        #endregion

        //encode uri
        //https://api.geoapify.com/v1/geocode/search?text=8G4QVJ6W%2BP4&apiKey=KEY_HERE

        #region make api call - do i need to urlencode request.address?
        var resource = PlusCodeValidator.IsValidPlusCode(request.address)
          ? $"v1/geocode/search?text={HttpUtility.UrlEncode(request.address)}&apiKey={request.key}"
          : $"v1/geocode/search?text={request.address}&apiKey={request.key}"
        ;
        var client = new RestClient("https://api.geoapify.com/");
        var restRequest = new RestRequest {
          Resource = resource,
          Method = Method.Get,
          RequestFormat = DataFormat.Json,
        };
        //if (wp != null) {
        //  client.Proxy = wp;
        //}
        var restResponse = await client.ExecuteAsync(restRequest);
        #endregion

        #region response validation
        if (restResponse.ErrorException != null) {
          return response = new Response { status = restResponse.ErrorException.Message };
        }
        if (restResponse.StatusCode != HttpStatusCode.OK) {
          return response = new Response { status = $"status was {restResponse.StatusCode}" };
        }
        if (restResponse.ErrorException != null && !string.IsNullOrWhiteSpace(restResponse.ErrorException.Message)) {
          return response = new Response { status = $"rest call had error exception: {restResponse.ErrorException.Message}" };
        }
        if (string.IsNullOrWhiteSpace(restResponse.Content)) {
          return response = new Response { status = "content was empty" };
        }
        #endregion

        response.jsonResult = restResponse.Content;
        dynamic json = JObject.Parse(restResponse.Content);
        if (json.features.Count == 0) {
          return response = new Response { status = "no features found in response" };
        }

        #region parse response
        response.lat = json.features[0].properties?.lat?.Value;
        response.lng = json.features[0].properties?.lon?.Value;
        response.rankMatchType = json.features[0].properties?.rank?.match_type?.Value;


        var addressLine1 = json.features[0].properties?.address_line1?.Value;
        var addressLine2 = json.features[0].properties?.address_line2?.Value;
        response.streetAddress = string.Join(", ", new List<string> { addressLine1, addressLine2 }.Where(x => !string.IsNullOrWhiteSpace(x)));
        response.normalizedFormattedAddress = json.features[0].properties?.formatted?.Value;

        response.city = json.features[0].properties?.city?.Value;
        response.province = json.features[0].properties?.state?.Value;
        response.postalCode = json.features[0].properties?.postcode?.Value;
        response.countryCode = json.features[0].properties?.country_code?.Value;
        response.resultType = json.features[0].properties?.result_type?.Value;
        response.category = json.features[0].properties?.category?.Value;
        response.plusCode = json.features[0].properties?.plus_code?.Value;
        #endregion

        #region latitude and longitude not found
        if (response.lng.Equals(0) || response.lat.Equals(0)) {
          return response = new Response { status = "latitude and longitude ZERO" };
        }
        #endregion
        #region if nromalizedFormattedAddress is empty, set it to the original address
        if (string.IsNullOrWhiteSpace(response.normalizedFormattedAddress)) {
          response.normalizedFormattedAddress = request.address;
        }
        #endregion
        #region fail if address is empty
        if (string.IsNullOrWhiteSpace(response.streetAddress)) {
          return response = new Response { status = "unable to geocode address (streetAddress was empty)" };
        }
        #endregion
        #region fail if normalizedFormattedAddress is empty
        if (string.IsNullOrWhiteSpace(response.normalizedFormattedAddress)) {
          return response = new Response { status = "unable to geocode address (normalizedFormattedAddress was empty)" };
        }
        #endregion

        response.httpStatusCode = HttpStatusCode.OK;
        return response;
      } catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          return response = new Response { status = Constants.ErrorMessages.CANCELLATION_REQUESTED_STATUS };
        }
        LogIt.E(ex);
        return response = new Response { httpStatusCode = HttpStatusCode.InternalServerError, status = Constants.ErrorMessages.UNEXPECTED_ERROR_STATUS };
      } finally {
        dtLastRequest = DateTime.UtcNow;
        request.key = "DO_NOT_LOG";
        LogIt.I(JsonConvert.SerializeObject(
          new {
            response.httpStatusCode,
            response.status,
            request?.address,
            //response?.coordinate,
          }, Formatting.Indented));
      }
    }
  }
}




