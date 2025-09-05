using System;
using System.Net;
using System.Threading.Tasks;
using MoarUtils.commands.logging;
using RestSharp;

namespace MoarUtils.Utils.Slack {
  public class PublishToChannel {
    public const string BASE = "https://hooks.slack.com";
    public const string DEFAULT_HEADER = "BORP";

    public static void ExecuteAsync(
      string content,
      string resource,
      string header = DEFAULT_HEADER,
      bool spoofUserAgent = false
    ) {
      try {
        Task.Factory.StartNew(() => {
          try {
            if (!ExecuteSync(content: content, resource: resource, header: header, spoofUserAgent: spoofUserAgent)) {
              LogIt.W("unable to publish to slack");
            }
          } catch (Exception ex) {
            LogIt.E(ex);
          }
        });
      } catch (Exception ex) {
        LogIt.E(ex);
      }
    }


    public static bool ExecuteSync(
      string content,
      string resource,
      string header = DEFAULT_HEADER,
      bool spoofUserAgent = false
    ) {
      //TODO: do this async in task
      try {
        //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        var h = "----------" + (string.IsNullOrWhiteSpace(header) ? DEFAULT_HEADER : header.ToUpper()) + "----------" + "\n";
        var client = new RestClient(BASE);
        //client.ClientCertificates = new X509CertificateCollection() { certificate };

        var request = new RestRequest {
          Resource = resource,
          Method = Method.Post,
          RequestFormat = DataFormat.Json,
        };


        if (spoofUserAgent) {
          request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36");
        }
        content = h + content.Replace("\"", "'");
        //was: .AddBody
        request.AddJsonBody(new {
          text = content
        });

        var response = client.ExecuteAsync(request).Result;
        //var content = response.Content;
        if (response.ErrorException != null) {
          LogIt.E(response.ErrorException);
          throw response.ErrorException;
        }
        if (response.StatusCode != HttpStatusCode.OK) {
          LogIt.W(response.StatusCode);
        }
        return (response.StatusCode == HttpStatusCode.OK);
      } catch (Exception ex) {
        LogIt.E(ex);
      }
      return false;
    }
  }
}
