using System;
using MoarUtils.commands.logging;
using RestSharp;

namespace MoarUtils.Utils.DisposableEmailCheck {
  public class BlockDisposableEmail {
    public static bool IsDisposable(string email, string apiKey) {
      try {
        var client = new RestClient(new RestClientOptions {
          BaseUrl = new Uri("http://check.block-disposable-email.com/"),
          MaxTimeout = 5000
          //Timeout = 5000
        });
        var response = client.ExecuteAsync(new RestRequest {
          Resource = "easyapi/json/" + apiKey + "/" + email,
          Method = Method.Get,
          //RequestFormat = RestSharp.DataFormat.Json
        }).Result;
        var content = response.Content;
        dynamic json = Newtonsoft.Json.Linq.JObject.Parse(content);

        //{"request_status":"success","domain_status":"block","version":"0.1","servertime":"2013-01-14 22:37:39","server_id":"mirror2_chicago"}
        var request_status = json.request_status.Value;
        var domain_status = json.domain_status.Value;

        var disposable = (request_status == "success") && (domain_status == "block");

        //consider fallback plan if/when we run out of queries... ie use the static list.

        return disposable;
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }
  }
}
