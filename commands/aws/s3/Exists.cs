using System;
using Amazon;
using Amazon.S3;
using MoarUtils.commands.logging;
using Newtonsoft.Json;

namespace MoarUtils.Utils.AWS.S3 {
  public class Exists {
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string key,
      string bucketName,
      RegionEndpoint re,
      out string url
    ) {
      var exists = false;
      url = "";
      try {
        using (var s3c = new AmazonS3Client(
          awsAccessKeyId: AWSAccessKey,
          awsSecretAccessKey: AWSSecretKey,
          region: re
        )) {
          var s3FileInfo = new Amazon.S3.IO.S3FileInfo(s3c, bucketName, key);
          exists = s3FileInfo.Exists;
          if (exists) {
            url = "https://s3.amazonaws.com/" + bucketName + "/" + key;
          }
          return exists;
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      } finally {
        LogIt.I(JsonConvert.SerializeObject(new {
          exists,
          url,
        }, Formatting.Indented));
      }
    }
  }
}
