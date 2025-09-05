using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.AWS.S3 {
  public class Copy {
    public static bool Execute(
      string AWSAccessKey,
      string AWSSecretKey,
      RegionEndpoint re,
      string sourceBucketName,
      string sourceKey,
      string destBucketName,
      string destKey,
      S3CannedACL s3ca,
      string contentType = null
    //out long? fileLengthBytes
    ) {
      //note: not vlidating cannced acls
      //fileLengthBytes = null;
      try {
        //validate exists
        string sourceUrl;
        if (!Exists.Execute(
          key: sourceKey,
          bucketName: sourceBucketName,
          url: out sourceUrl,
          re: re,
          AWSAccessKey: AWSAccessKey,
          AWSSecretKey: AWSSecretKey
        )) {
          LogIt.W("source did not exist:" + sourceBucketName + "|" + sourceKey);
          return false;
        }

        //validate dest doesn't already exist, fail if it does bc we aren't validating that it is different? maybe shar eeach in future?
        string destUrl;
        if (Exists.Execute(
          key: destKey,
          bucketName: destBucketName,
          url: out destUrl,
          re: re,
          AWSAccessKey: AWSAccessKey,
          AWSSecretKey: AWSSecretKey
        )) {
          LogIt.W("dest existed already:" + sourceBucketName + "|" + sourceKey);
          return false;
        }

        //copy 
        using (var s3c = new AmazonS3Client(
          awsAccessKeyId: AWSAccessKey,
          awsSecretAccessKey: AWSSecretKey,
          region: re
        )) {
          var request = new CopyObjectRequest {
            SourceBucket = sourceBucketName,
            SourceKey = sourceKey,
            DestinationBucket = destBucketName,
            DestinationKey = destKey,
            CannedACL = s3ca
          };
          if (!string.IsNullOrWhiteSpace(contentType)) {
            request.MetadataDirective = S3MetadataDirective.REPLACE;
            request.ContentType = contentType;
          }
          var response = s3c.CopyObject(request);
          return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
          //fileLengthBytes = cor.
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }
  }
}


