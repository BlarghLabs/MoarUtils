using System;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.AWS.S3 {
  public class Upload {
    public const string S3_BASE = "https://s3.amazonaws.com/";
    public static bool Exists(
      string AWSAccessKey,
      string AWSSecretKey,
      string fileKey,
      string bucketName,
      RegionEndpoint re,
      out string url
    ) {
      url = "";
      try {
        using (var s3c = new AmazonS3Client(AWSAccessKey, AWSSecretKey, re)) {
          var gomr = s3c.GetObjectMetadata(new GetObjectMetadataRequest {
            BucketName = bucketName,
            Key = fileKey
          });
          url = S3_BASE + bucketName + "/" + fileKey;
          return gomr.ContentLength != 0;
          //return true;
        }
      } catch (Amazon.S3.AmazonS3Exception ex) {
        if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
          return false;

        //status wasn't not found, so throw the exception
        throw;
      }
    }

    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string bucketName,
      string filePath,
      string toPath,
      RegionEndpoint re,
      out string url,
      S3CannedACL s3ca
    ) {
      url = "";
      try {
        using (var tu = new TransferUtility(AWSAccessKey, AWSSecretKey, re)) {
          var tuur = new TransferUtilityUploadRequest {
            FilePath = filePath,
            BucketName = bucketName,
            Key = toPath,
            CannedACL = s3ca
          };
          tu.Upload(tuur);
          url = S3_BASE + bucketName + "/" + toPath;
        }
        return true;
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }

    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string bucketName,
      byte[] ba,
      string toPath,
      out string url,
      RegionEndpoint re,
      S3CannedACL s3ca
    ) {
      url = "";
      try {
        using (var ms = new MemoryStream(ba)) {
          var uploadMultipartRequest = new TransferUtilityUploadRequest {
            BucketName = bucketName,
            Key = toPath,
            CannedACL = s3ca,
            InputStream = ms,
            //PartSize = 123?
          };

          using (var tu = new TransferUtility(AWSAccessKey, AWSSecretKey, re)) {
            tu.Upload(uploadMultipartRequest);
          }
          url = S3_BASE + bucketName + "/" + toPath;
          return true;
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }

    //TODO: do i really want all these to be public read?! are we locking down elsewhre w/ cors or bucket policy?
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      string bucketName,
      MemoryStream ms,
      string key,
      RegionEndpoint re,
      out string url
    ) {
      url = "";
      try {
        var uploadMultipartRequest = new TransferUtilityUploadRequest {
          BucketName = bucketName,
          Key = key,
          CannedACL = S3CannedACL.PublicRead,
          InputStream = ms,
        };

        using (var tu = new TransferUtility(AWSAccessKey, AWSSecretKey, re)) {
          tu.Upload(uploadMultipartRequest);
        }
        url = S3_BASE + bucketName + "/" + key;
        return true;
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }
  }
}


