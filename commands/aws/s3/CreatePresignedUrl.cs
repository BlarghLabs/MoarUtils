using System;
using Amazon.S3;
using Amazon.S3.Model;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.AWS.S3 {
  public class CreatePresignedUrl {
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string bucketName,
      string objectKey,
      out string url,
      int numberOfMinutes = 30
    ) {
      url = "";
      try {
        using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1)) {
          var gpsur = new GetPreSignedUrlRequest {
            BucketName = bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.AddMinutes(numberOfMinutes)
          };
          url = s3Client.GetPreSignedURL(gpsur);
        }
        return true;
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }
  }
}


