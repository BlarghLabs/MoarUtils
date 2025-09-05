using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.AWS.S3 {
  public class Delete {
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string bucketName,
      string key,
      RegionEndpoint re
    ) {
      try {
        using (var s3c = new AmazonS3Client(AWSAccessKey, AWSSecretKey, re)) {
          var dor = s3c.DeleteObject(new DeleteObjectRequest {
            BucketName = bucketName,
            Key = key
          });
          return dor.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }
  }
}


