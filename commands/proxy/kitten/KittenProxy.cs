using System;
using System.Collections.Generic;
using System.Net;
using MoarUtils.commands.logging;
using RestSharp;

namespace MoarUtils.commands.proxy.kitten {
  public class GetUsableWebProxies {
    public const int defaultMinimumUptimePercentage = 65;

    public static List<WebProxy> Execute(
      string apiKey,
      string apiHost,
      int minimumUptimePercentage = defaultMinimumUptimePercentage,
      int maxResults = 10,
      int minLatencySeconds = 4,
      bool anonymous = false,
      string limitProxyTypes = "https"
      ) {
      try {
        Utils.Cert.IgnoreInvalidCerts(); //only req while self signed
        var rc = new RestClient(apiHost);
        var rr = rc.ExecuteAsync(
          new RestRequest {
            Resource = "api/Proxy/get?apiKey=" + apiKey
                        + "&limitProxyTypes=" + limitProxyTypes
                        + "&minUptime=" + minimumUptimePercentage
                        + "&pageSize=" + maxResults
                        + "&sortBy=latestTest"
                        + "&active=true"
                        + "&minLatency=" + minLatencySeconds
                        + (!anonymous ? "" : "&anonymous=true"),
            Method = Method.Get,
            RequestFormat = DataFormat.Json,
          }).Result;
        if (rr.ErrorException != null) {
          throw rr.ErrorException;
        }
        if (rr.StatusCode != HttpStatusCode.OK) {
          throw new Exception(rr.StatusCode.ToString() + "|" + rr.Content);
        }
        var content = rr.Content;
        dynamic json = Newtonsoft.Json.Linq.JObject.Parse(content);
        string status = json.status.Value;

        var lowp = new List<WebProxy>();
        foreach (var p in json.proxies) {
          lowp.Add(new WebProxy { Address = new Uri("http://" + p.host.Value) });
        }
        return lowp;
      } catch (Exception ex) {
        LogIt.E(ex);
        throw ex;
      }
    }
  }
}
