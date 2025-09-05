using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.AWS.S3 {
  public class UpdateContentType {
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      RegionEndpoint re,
      string bucketName,
      string key,
      S3CannedACL s3ca,
      string contentType
    ) {
      try {
        using (var s3c = new AmazonS3Client(
          awsAccessKeyId: AWSAccessKey,
          awsSecretAccessKey: AWSSecretKey,
          region: re
        )) {
          var request = new CopyObjectRequest {
            SourceBucket = bucketName,
            SourceKey = key,
            DestinationBucket = bucketName,
            DestinationKey = key,
            CannedACL = s3ca,
            ContentType = contentType
          };

          var response = s3c.CopyObject(request);
          return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }
  }
}


