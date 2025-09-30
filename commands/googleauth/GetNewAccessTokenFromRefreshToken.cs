using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MoarUtils.commands.logging;
using MoarUtils.models.commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace MoarUtils.Utils.GoogleAuth {
  public class GetNewAccessTokenFromRefreshToken {
    public class Request {
      public string clientId { get; set; }
      public string clientSecret { get; set; }
      public string refreshToken { get; set; }
    }

    public class Response : ResponseStatusModel {

      //{
      //  "access_token" : "XXX",
      //  "token_type" : "Bearer",
      //  "expires_in" : 3600,
      //  "refresh_token" : "1/XXX"
      //}

      public string access_token { get; set; }
      public string token_type { get; set; }
      public string refresh_token { get; set; }
      public int expires_in { get; set; }
      public string jsonResult { get; set; }
    }

    /// <summary>
    /// if successed, should save accesstoken and expire and use until expires
    /// if fails shoudl consider clearing refresh token
    /// </summary>
    public static async Task<Response> Execute(
      Request request,
      CancellationToken cancellationToken
    ) {
      var response = new Response { };

      try {
        var client = new RestClient(new RestClientOptions {
          BaseUrl = new Uri("https://accounts.google.com"),
        });
        //https://developers.google.com/accounts/docs/OAuth2WebServer
        //#Using a Refresh Token
        var restRequest = new RestRequest {
          Resource = "o/oauth2/token",
          Method = Method.Post,
        };
        restRequest.AddParameter("client_id", request.clientId);
        restRequest.AddParameter("client_secret", request.clientSecret);
        restRequest.AddParameter("refresh_token", request.refreshToken);
        restRequest.AddParameter("grant_type", "refresh_token");
        restRequest.AddParameter("Content-Type", "application/x-www-form-urlencoded");
        //var restResponse = await client.ExecuteAsync(restRequest).ConfigureAwait(false);
        var restResponse = client.ExecuteAsync(restRequest, cancellationToken).Result;
        //valid response: { "access_token":"1/XXX", "expires_in":3920, "token_type":"Bearer",}
        if (restResponse.StatusCode != HttpStatusCode.OK) {
          return response = new Response { status = "StatusCode was " + restResponse.StatusCode };
        }

        response.jsonResult = restResponse.Content;
        //LogIt.D(content);
        dynamic json = JObject.Parse(restResponse.Content);
        response.access_token = json.access_token;
        response.expires_in = json.expires_in;
        response.token_type = json.token_type;
        response.refresh_token = json.refresh_token;

        //if (string.IsNullOrEmpty(tr.access_token)) {
        //  //should I clear out refesh token?
        //  LogIt.E("access token was null");
        //  //} else {
        //  //  icalgenie.lib.Db.Credential.AddOrUpdateUserCredentials(new credential_element { name = CredentialElementName.GoogleAccessToken.ToString(), value = tr.access_token }, u, SourceType.GmailAccount);
        //  //}
        //}

        response.httpStatusCode = HttpStatusCode.OK;
        return response;
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
            //request,
            //r,
          }));
      }
    }
  }
}
