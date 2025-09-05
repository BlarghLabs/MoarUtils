using System;
using System.Net;
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

//http://www.mapquestapi.com/geocoding/v1/address?key=KEY_HERE&location=lancaster%20pa
//http://www.mapquestapi.com/geocoding/v1/address?key=KEY_HERE&callback=renderOptions&inFormat=kvp&outFormat=json&location=Lancaster,PA

//API Info:

//Rate Limiting

namespace moarutils.utils.gis.geocode {
  public static class ViaMapQuest {
    public class Request {
      public string address { get; set; }
      public string key { get; set; }
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
        if (string.IsNullOrWhiteSpace(request.address) || request.address.Equals("0")) {
          return response = new Response { status = "address is empty" };
        }

        var uea = HttpUtility.UrlEncode(request.address.Trim());
        var client = new RestClient("https://www.mapquestapi.com/");
        //var client = new RestClient("https://developer.mapquest.com/");
        //var client = new RestClient("http://www.mapquestapi.com/");
        var restRequest = new RestRequest(
          //resource: "/geocoding/v1/address?key=" + key + "&location=" + uea,
          resource: "/geocoding/v1/address?key=" + request.key + "&location=" + uea,
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
        var sc = (json.info.statuscode == null) ? -1 : json.info.statuscode.Value;
        if (sc != 0) {
          return response = new Response {
            status = (json.info.messages == null)
            ? ""
            : json.info.messages[0].Value
          };
        }

        var lng = Convert.ToDecimal(json.results[0].locations[0].latLng.lng.Value);
        var lat = Convert.ToDecimal(json.results[0].locations[0].latLng.lat.Value);
        var pc = json.results[0].locations[0].geocodeQualityCode.Value; //A1XAX and A3XAX are bad country level precision
        if ((lat != 0) && (lng != 0) && (pc != "A1XAX") && (pc != "A3XAX")) {
          //TODO: validate range
          return response = new Response {
            coordinate = new Coordinate {
              geocoder = Geocoder.MapQuest,
              lat = lat,
              lng = lng,
              precision = pc,
            },
            httpStatusCode = HttpStatusCode.OK,
          };
        }

        return response = new Response { status = "not found" };
      } catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          return response = new Response { status = Constants.ErrorMessages.CANCELLATION_REQUESTED_STATUS };
        }
        LogIt.E(ex);
        return response = new Response { status = Constants.ErrorMessages.UNEXPECTED_ERROR_STATUS, httpStatusCode = HttpStatusCode.InternalServerError };
      } finally {
        LogIt.I(JsonConvert.SerializeObject(
          new {
            response.httpStatusCode,
            response.status,
            request?.address,
            response?.coordinate,
          }, Formatting.Indented));
      }
    }
  }
}