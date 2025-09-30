using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MoarUtils.commands.logging;
using MoarUtils.models.commands;
using Newtonsoft.Json;
using RestSharp;

namespace MoarUtils.Utils.GoogleAuth {
  public class GetUserInfoFromAccessToken {
    public class Request {
      public string clientId { get; set; }
      public string clientSecret { get; set; }
      public string accessToken { get; set; }
    }

    public class Response : ResponseStatusModel {
      public string email { get; set; }
      public string picture { get; set; }
      public string id { get; set; }
      public string jsonContent { get; set; }
      public bool verifiedEmail { get; set; }
    }

    public static async Task<Response> Execute(
      Request request,
      CancellationToken cancellationToken
    ) {
      var response = new Response { };
      try {
        if (string.IsNullOrEmpty(request.accessToken)) {
          return response = new Response { status = "access token is required" };
        }
        var client = new RestClient("https://www.googleapis.com/");
        var restRequest = new RestRequest("oauth2/v2/userinfo", Method.Get);
        restRequest.AddHeader("Authorization", "Bearer " + request.accessToken); //Authorization: Bearer XXX
        //var restResponse = await client.ExecuteAsync(restRequest).ConfigureAwait(false);
        var restResponse = client.ExecuteAsync(restRequest, cancellationToken).Result;

        if (restResponse.StatusCode != HttpStatusCode.OK) {
          return response = new Response { status = $"StatusCode was {restResponse.StatusCode}" };
        }
        if (restResponse.ErrorException != null) {
          return response = new Response { status = $"response had error exception: {restResponse.ErrorException.Message}" };
        }
        if (string.IsNullOrWhiteSpace(restResponse.Content)) {
          return response = new Response { status = "response content was empty" };
        }

        response.jsonContent = restResponse.Content;
        dynamic json = JsonConvert.DeserializeObject(restResponse.Content);

        #region cheat sheet
        //{
        //  {
        //    "id": "123456789012345678901",
        //    "email": "foo@bar.baz",
        //    "verified_email": true,
        //    "picture": "https://lh3.googleusercontent.com/a-/1111111111111111111111111111111111111111111111"
        // }
        //}
        #endregion

        response.email = json.email == null
          ? null
          : json.email.Value
        ;
        response.picture = json.picture == null
          ? null
          : json.picture.Value
        ;
        response.id = json.id == null
          ? null
          : json.id.Value
        ;
        response.verifiedEmail = json.verified_email == null
          ? false
          : json.verified_email.Value
        ;


        response.httpStatusCode = HttpStatusCode.OK;
        return response;
      } catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          return response = new Response { status = Constants.ErrorMessages.CANCELLATION_REQUESTED_STATUS };
        }
        LogIt.E(ex);
        return response = new Response { status = Constants.ErrorMessages.UNEXPECTED_ERROR_STATUS, httpStatusCode = HttpStatusCode.InternalServerError };
      } finally {
        LogIt.I(JsonConvert.SerializeObject(new {
          response.httpStatusCode,
          response.status,
          //request, //logging
          response?.jsonContent, //logging
          response?.email,
          response?.picture,
          response?.verifiedEmail,
        }, Formatting.Indented));
      }
    }
  }
}

