using System;
using System.Collections.Generic;
using System.Globalization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Vision.v1;
using Google.Apis.Vision.v1.Data;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.images {
  public class GoogleVisionApi {
    public static bool Detect(
      string googe_service_account_json,
      string applicationName,
      string base64Image,
      out string summary
    ) {
      summary = "";

      try {
        var vs = GetVisionServiceByServiceAccount(
          googe_service_account_json: googe_service_account_json,
          applicationName: applicationName

        );
        var loair = DetectLabels(vs, base64Image);
        // Check if label annotations were found
        if (loair != null) {
          // Loop through and output label annotations for the image
          var lod = new List<string> { };
          foreach (var air in loair) {
            foreach (var ea in air.LabelAnnotations) {
              var p = ((decimal)ea.Score).ToString("P1", CultureInfo.InvariantCulture);
              lod.Add(ea.Description + " (" + p + ")");
            }
          }
          //var lod = loair.Select( 
          // air => air.LabelAnnotations.Select(
          //    ea=> ea.Description + " (" + (Math.Round((decimal)ea.Score,2) * 100).ToString() + "%)" 
          //   )
          //   ).ToList();
          summary = string.Join(", ", lod);
        } else {
          LogIt.D("No labels found.");
        }
        return true;
      } catch (Exception ex) {
        LogIt.E(ex);
      }
      return false;

    }

    /// <summary>
    /// Detect labels for an image using the Cloud Vision API.
    /// </summary>
    /// <param name="vision">an authorized Cloud Vision client.</param>
    /// <param name="base64Image">Base64 encoded for JSON ASCII text based request</param>
    /// <returns>a list of labels detected by the Vision API for the image.
    /// </returns>
    public static IList<AnnotateImageResponse> DetectLabels(
      VisionService vision,
      string base64Image
    ) {
      // Post label detection request to the Vision API
      var responses = vision.Images.Annotate(
          new BatchAnnotateImagesRequest {
            Requests = new[] {
                    new AnnotateImageRequest() {
                        Features = new [] {
                        new Feature {
                          Type = "LABEL_DETECTION"
                        }},
                        Image = new Image {
                          Content = base64Image
                        }
                    }
               }
          }).Execute();
      return responses.Responses;
    }

    private static VisionService GetVisionServiceByServiceAccount(
      string googe_service_account_json,
      string applicationName
    ) {
      string[] scopes = new string[] { VisionService.Scope.CloudPlatform }; // Put your scopes here
      using (var s = Streams.GenerateStreamFromString(googe_service_account_json)) {
        var credential = GoogleCredential.FromStream(s);
        credential = credential.CreateScoped(scopes);

        var service = new VisionService(new BaseClientService.Initializer() {
          HttpClientInitializer = credential,
          ApplicationName = applicationName,
        });

        return service;
      }
    }
  }
}
