using System;
using System.Net;
using MoarUtils.commands.logging;
using MoarUtils.models.gis;
using RestSharp;

namespace moarutils.utils.gis.reversegeocode {

  //https://maps.googleapis.com/maps/api/geocode/json?latlng=40.714224,-73.961452&key=YOUR_API_KEY
  public static class ViaGoogle {
    public static bool Execute(
      decimal lat,
      decimal lng,
      string apiKey,
      out AddressFields af
    ) {
      af = new AddressFields { };
      try {
        var resource = "maps/api/geocode/json?latlng=" + lat.ToString() + "," + lng.ToString() + "&key=" + apiKey;
        var client = new RestClient("https://maps.googleapis.com/");
        var request = new RestRequest(resource, Method.Get);
        request.RequestFormat = DataFormat.Json;
        var response = client.ExecuteAsync(request).Result;
        if (response.ErrorException != null) {
          LogIt.W(response.ErrorException);
        }
        if (response.StatusCode != HttpStatusCode.OK) {
          LogIt.W(response.StatusCode);
        }
        var content = response.Content;
        dynamic json = Newtonsoft.Json.Linq.JObject.Parse(content);
        dynamic address_components = json.results[0].address_components;

        for (int i = 0; i < address_components.Count; i++) {
          var ac = address_components[i];
          var lot = ac.types;
          for (int j = 0; j < ac.types.Count; j++) {
            var t = (string)ac.types[j].Value;
            if (t.Equals("street_number")) {
              af.line1 = ac.short_name.Value;
              break;
            }
          }
          for (int j = 0; j < ac.types.Count; j++) {
            var t = (string)ac.types[j].Value;
            if (t.Equals("route")) {
              af.street = ac.short_name.Value;
              break;
            }
          }
          for (int j = 0; j < ac.types.Count; j++) {
            var t = (string)ac.types[j].Value;
            if (t.Equals("sublocality")) {
              af.city = ac.short_name.Value;
              break;
            }
          }
          for (int j = 0; j < ac.types.Count; j++) {
            var t = (string)ac.types[j].Value;
            if (t.Equals("administrative_area_level_1")) {
              af.statecode = ac.short_name.Value;
              af.state = ac.long_name.Value;
              break;
            }
          }
          for (int j = 0; j < ac.types.Count; j++) {
            var t = (string)ac.types[j].Value;
            if (t.Equals("country")) {
              af.countrycode = ac.short_name.Value;
              break;
            }
          }
          for (int j = 0; j < ac.types.Count; j++) {
            var t = (string)ac.types[j].Value;
            if (t.Equals("postal_code")) {
              af.postal = ac.short_name.Value;
              break;
            }
          }

        }
        return true;
      } catch (Exception ex) {
        LogIt.W(ex.Message + "|" + lat + "|" + lng);
      }
      return false;
    }
  }
}
