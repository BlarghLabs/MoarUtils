using System;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.AWS.S3 {
  public class Get {
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string bucketName,
      string fileKey,
      RegionEndpoint re,
      out MemoryStream ms
    ) {
      ms = null;
      try {
        using (var s3c = new AmazonS3Client(AWSAccessKey, AWSSecretKey, re)) {
          var response = s3c.GetObject(
            new GetObjectRequest {
              BucketName = bucketName,
              Key = fileKey
            });
          using (var rs = response.ResponseStream) {
            ms = new MemoryStream();
            rs.CopyTo(ms);
            return true;
          }
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }

    //super inefficient? ugh
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string bucketName,
      string fileKey,
      RegionEndpoint re,
      out byte[] ba
    ) {
      ba = null;
      try {
        using (var s3c = new AmazonS3Client(AWSAccessKey, AWSSecretKey, re)) {
          var response = s3c.GetObject(
            new GetObjectRequest {
              BucketName = bucketName,
              Key = fileKey,
            });
          using (var rs = response.ResponseStream) {
            using (var ms = new MemoryStream()) {
              rs.CopyTo(ms);
              ba = ms.ToArray();
              return true;
            }
          }
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }
  }
}
