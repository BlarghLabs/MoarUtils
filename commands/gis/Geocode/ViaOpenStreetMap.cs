using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
  public static class ViaOpenStreetMap {

    public class Request {
      public string address { get; set; }
      public string userAgent { get; set; } = "MoarUtils Geocoder/1.0"; // Required by Nominatim
      public int maxTriesIfQueryLimitReached { get; set; } = 1;
      public int limit { get; set; } = 1; // Number of results to return
      public string countryCode { get; set; } // Optional country code filter (e.g., "us", "ca")
      public bool addressDetails { get; set; } = false; // Include detailed address components
    }

    public class Response : ResponseStatusModel {
      public Coordinate coordinate { get; set; }
      public string displayName { get; set; }
      public string osmType { get; set; }
      public long osmId { get; set; }
      public string category { get; set; }
      public string type { get; set; }
      public decimal importance { get; set; }
    }

    private const double m_dThrottleSeconds = 1.1; // Nominatim requires max 1 request per second
    private static DateTime dtLastRequest = DateTime.UtcNow;
    private static Mutex mLastRequest = new Mutex();

    private static string BuildQueryString(Request request) {
      var queryParams = new List<string>();

      if (!string.IsNullOrEmpty(request.address)) {
        queryParams.Add($"q={Uri.EscapeDataString(CondenseWhiteSpace.Execute(request.address))}");
      }

      queryParams.Add("format=json");
      queryParams.Add($"limit={request.limit}");

      if (!string.IsNullOrEmpty(request.countryCode)) {
        queryParams.Add($"countrycodes={request.countryCode.ToLower()}");
      }

      if (request.addressDetails) {
        queryParams.Add("addressdetails=1");
      }

      return string.Join("&", queryParams);
    }

    public static async Task<Response> Execute(
      Request request,
      CancellationToken cancellationToken,
      WebProxy wp = null
    ) {
      lock (mLastRequest) {
        // Force delay to respect Nominatim rate limits (max 1 request per second)
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
        LogIt.E("unable to geocode via OpenStreetMap");
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

        if (string.IsNullOrEmpty(request.userAgent)) {
          return response = new Response { status = "userAgent required for OpenStreetMap Nominatim" };
        }
        if (cancellationToken.IsCancellationRequested) {
          return response = new Response { status = Constants.ErrorMessages.CANCELLATION_REQUESTED_STATUS };
        }

        int trys = 1;
        do {
          var client = new RestClient("https://nominatim.openstreetmap.org/");
          var restRequest = new RestRequest(
            resource: $"search?{BuildQueryString(request)}",
            method: Method.Get
          );

          // Nominatim requires a User-Agent header
          restRequest.AddHeader("User-Agent", request.userAgent);

          //if (wp != null) {
          //  client.Proxy = wp;
          //}

          var restResponse = await client.ExecuteAsync(restRequest);

          if (restResponse.ErrorException != null) {
            return response = new Response { status = $"response had error exception: {restResponse.ErrorException.Message}" };
          }

          //if (restResponse.StatusCode == HttpStatusCode.TooManyRequests) {
          //  if (trys < request.maxTriesIfQueryLimitReached) {
          //    Thread.Sleep(2000 * trys); // Wait longer for rate limit
          //    trys++;
          //    continue;
          //  }
          //  return response = new Response { status = "rate limit exceeded" };
          //}

          if (restResponse.StatusCode != HttpStatusCode.OK) {
            return response = new Response { status = $"StatusCode was {restResponse.StatusCode}" };
          }

          if (string.IsNullOrWhiteSpace(restResponse.Content)) {
            return response = new Response { status = "content was empty" };
          }

          var content = restResponse.Content;

          try {
            var jsonArray = JArray.Parse(content);

            if (jsonArray.Count == 0) {
              return response = new Response { status = "ZERO_RESULTS" };
            }

            var firstResult = jsonArray[0];

#if DEBUG
            Console.WriteLine(firstResult);
#endif

            var lat = firstResult["lat"]?.ToString();
            var lon = firstResult["lon"]?.ToString();
            var displayName = firstResult["display_name"]?.ToString();
            var osmType = firstResult["osm_type"]?.ToString();
            var osmId = firstResult["osm_id"]?.Value<long>() ?? 0;
            var category = firstResult["category"]?.ToString();
            var type = firstResult["type"]?.ToString();
            var importance = firstResult["importance"]?.Value<decimal>() ?? 0;

            if (string.IsNullOrWhiteSpace(lat) || string.IsNullOrWhiteSpace(lon)) {
              return response = new Response { status = "invalid coordinates in response" };
            }

            //var precision = DeterminePrecision(
            //  osmType: osmType,
            //  classField: firstResult["class"]?.ToString(),
            //  type: type,
            //  placeRank: firstResult["place_rank"]?.Value<int>() ?? 30,
            //  addressRank: firstResult["address_rank"]?.Value<int>() ?? 30,
            //  importance: importance
            //);
            // Determine precision based on OSM type and category
            //string precision = DeterminePrecision(osmType, category, type);
            var precision = string.Join("_", new List<string> { firstResult["class"]?.ToString(), firstResult["type"]?.ToString() }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return response = new Response {
              httpStatusCode = HttpStatusCode.OK,
              coordinate = new Coordinate {
                geocoder = Geocoder.OpenStreetMap,
                lat = Convert.ToDecimal(lat),
                lng = Convert.ToDecimal(lon),
                precision = precision
              },
              displayName = displayName,
              osmType = osmType,
              osmId = osmId,
              category = category,
              type = type,
              importance = importance
            };

          } catch (JsonException ex) {
            return response = new Response { status = $"JSON parsing error: {ex.Message}" };
          }

        } while (trys < request.maxTriesIfQueryLimitReached);

        return response = new Response {
          status = "unable to geocode after retries",
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
          response?.displayName
        }, Formatting.Indented));
      }
    }

    private static string DeterminePrecision(string osmType, string classField, string type, int placeRank, int addressRank, decimal importance) {
      // Use address_rank for most precise determination (lower number = more precise)
      if (addressRank <= 30) {
        // Very precise - house numbers, buildings
        if (addressRank <= 28) return "ROOFTOP";
        if (addressRank <= 30) return "RANGE_INTERPOLATED";
      }

      // Use place_rank as secondary indicator
      if (placeRank <= 16) return "ROOFTOP";           // Building level
      if (placeRank <= 18) return "RANGE_INTERPOLATED"; // Street level
      if (placeRank <= 20) return "GEOMETRIC_CENTER";   // Local area

      // Fallback to class/type analysis
      if (string.IsNullOrEmpty(classField) || string.IsNullOrEmpty(type)) {
        return "APPROXIMATE";
      }

      switch (classField.ToLower()) {
        case "place":
          switch (type.ToLower()) {
            case "house":
            case "house_number":
              return "ROOFTOP";
            case "suburb":
            case "neighbourhood":
            case "quarter":
              return "GEOMETRIC_CENTER";
            case "city":
            case "town":
            case "village":
            case "hamlet":
              return "APPROXIMATE";
            case "postcode":
              return "RANGE_INTERPOLATED";
            default:
              return "APPROXIMATE";
          }
        case "highway":
          return "RANGE_INTERPOLATED";
        case "building":
        case "amenity":
          return "ROOFTOP";
        case "shop":
        case "office":
        case "tourism":
          return "GEOMETRIC_CENTER";
        case "landuse":
        case "natural":
        case "leisure":
          return "APPROXIMATE";
        default:
          // Use importance as final fallback
          if (importance > 0.7m) return "GEOMETRIC_CENTER";
          if (importance > 0.3m) return "RANGE_INTERPOLATED";
          return "APPROXIMATE";
      }
    }
  }
}

#region result
/*
 * [
  {
    "place_id": 321631063,
    "licence": "Data © OpenStreetMap contributors, ODbL 1.0. http://osm.org/copyright",
    "osm_type": "way",
    "osm_id": 238241022,
    "lat": "38.8976997",
    "lon": "-77.0365532",
    "class": "office",
    "type": "government",
    "place_rank": 30,
    "importance": 0.6863355973183977,
    "addresstype": "office",
    "name": "White House",
    "display_name": "White House, 1600, Pennsylvania Avenue Northwest, Downtown, Ward 2, Washington, District of Columbia, 20500, United States",
    "boundingbox": [
      "38.8974906",
      "38.8979110",
      "-77.0368537",
      "-77.0362519"
    ]
  }
]
 * */
#endregion