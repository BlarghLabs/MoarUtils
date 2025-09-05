using System;
using System.Diagnostics;
using System.IO;
using Amazon;
using MoarUtils.commands.logging;

namespace MoarUtils.Utils.AWS.S3 {
  public class CrossRegionCopy {
    /// <summary>
    /// requires aws cli to be configured
    /// </summary>
    /// <param name="sourceRegionEndpoint"></param>
    /// <param name="sourceBucketName"></param>
    /// <param name="sourceKey"></param>
    /// <param name="destRegionEndpoint"></param>
    /// <param name="destBucketName"></param>
    /// <param name="destKey"></param>
    /// <returns></returns>
    public static bool AwsCliCopyToUsEastIfDne(
      string AWSAccessKey,
      string AWSSecretKey,
      RegionEndpoint sourceRegionEndpoint,
      string sourceBucketName,
      string sourceKey,
      RegionEndpoint destRegionEndpoint,
      string destBucketName,
      string destKey
    ) {
      try {
        string destUrl;
        if (Exists.Execute(
          key: destKey,
          bucketName: destBucketName,
          url: out destUrl,
          re: destRegionEndpoint,
          AWSAccessKey: AWSAccessKey,
          AWSSecretKey: AWSSecretKey
        )) {
          return true;
        }

        string sourceUrl;
        if (!Exists.Execute(
          key: sourceKey,
          bucketName: sourceBucketName,
          url: out sourceUrl,
          re: sourceRegionEndpoint,
          AWSAccessKey: AWSAccessKey,
          AWSSecretKey: AWSSecretKey
        )) {
          LogIt.W("source did not exist:" + sourceBucketName + "|" + sourceKey);
          return false;
        }

        //execute copy
        //aws s3 sync s3://resurveys.original/45277802/ec92a4ed-8900-42d0-b8f4-31209130b38e/ s3://resurveys.original.useast/45277802/ec92a4ed-8900-42d0-b8f4-31209130b38e/ --source-region us-west-2 --region us-east-1 
        try {
          // Call WaitForExit and then the using-statement will close.
          var args = "/c aws s3 sync"
            + " s3://" + sourceBucketName + "/" + sourceKey.Replace(Path.GetFileName(sourceKey), "")
            + " s3://" + destBucketName + "/" + destKey.Replace(Path.GetFileName(destKey), "")
            + " --source-region " + sourceRegionEndpoint.SystemName
            + " --region " + destRegionEndpoint.SystemName;

          var psi = new ProcessStartInfo {
            CreateNoWindow = false,
            UseShellExecute = false,
            FileName = "cmd",
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            Arguments = args
          };
          using (var p = Process.Start(psi)) {
            //p.WaitForExit();
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            var err = p.StandardError.ReadToEnd();
            Console.WriteLine(err);
            p.WaitForExit();

            //p.Start();
            //while (!p.StandardOutput.EndOfStream) {
            //  LogIt.D(p.StandardOutput.ReadLine());
            //}
          }
        } catch (Exception ex) {
          LogIt.E(ex);
          return false;
        }

        //check
        var exists = Exists.Execute(
          key: destKey,
          bucketName: destBucketName,
          url: out destUrl, re: destRegionEndpoint,
          AWSAccessKey: AWSAccessKey,
          AWSSecretKey: AWSSecretKey
        );
        return exists;
      } catch (Exception ex) {
        LogIt.E(ex);
        throw;
      }
    }

  }
}
