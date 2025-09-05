using System;
using System.ComponentModel;
using System.Configuration;
using System.Net.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;
using MoarUtils.commands.logging;
using MoarUtils.enums;

namespace MoarUtils.Utils {
  public static class Email {
    public static void SendMessage(
      string fromAddress,
      string fromDisplayName,
      string toAddress,
      string toDisplayName,
      string subject,
      string body,
      string replyToAddress,
      string replyToDisplayName,
      string ccAddress,
      string ccDisplayName,
      EmailEngine ee,
      bool isHtml = true,
      bool sendAsync = false,
      bool throwOnError = true
    ) {
      if (string.IsNullOrWhiteSpace(toAddress) || string.IsNullOrWhiteSpace(fromAddress)) {
        throw new Exception("to or from were empty");
      } else {
        switch (ee) {
          //case EmailEngine.MailMergeLib:
          //  #region
          //  var mmm = new MailMergeMessage();

          //  mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, fromAddress, fromDisplayName, Encoding.UTF8));
          //  mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, toAddress, toDisplayName, Encoding.UTF8));

          //  mmm.Subject = subject.Trim();
          //  mmm.HtmlText = body;
          //  mmm.PlainText = mmm.ConvertHtmlToPlainText();

          //  mmm.CultureInfo = new System.Globalization.CultureInfo("en-US");
          //  mmm.CharacterEncoding = Encoding.UTF8;
          //  mmm.TextTransferEncoding = System.Net.Mime.TransferEncoding.Base64;
          //  mmm.BinaryTransferEncoding = System.Net.Mime.TransferEncoding.Base64;

          //  SendMessage(mmm, sendAsync, throwOnError);
          //  #endregion
          //  break;
          //case EmailEngine.Chilkat:
          //  #region
          //  var e = new Chilkat.Email();

          //  e.Body = body;
          //  e.Subject = subject;
          //  e.AddTo(toDisplayName, toAddress);
          //  e.FromAddress = fromAddress;
          //  e.FromName = fromDisplayName;

          //  SendMessage(e, sendAsync, throwOnError);
          //  #endregion
          //  break;
          case EmailEngine.DotNet:
            #region
            MailMessage mm = new MailMessage();

            mm.From = new MailAddress(fromAddress, fromDisplayName, System.Text.Encoding.UTF8);
            mm.Subject = subject.Trim();
            mm.SubjectEncoding = System.Text.Encoding.UTF8;
            mm.Body = body;
            mm.IsBodyHtml = isHtml;
            mm.BodyEncoding = System.Text.Encoding.UTF8;
            //mm.PlainText = mmm.ConvertHtmlToPlainText();

            #region handle comma delimeted TO
            if (toAddress.Contains(",")) {
              //Split mult. comma delim to Addresses
              string[] saTo = toAddress.Split((new char[] { ',', ';' }), StringSplitOptions.RemoveEmptyEntries);

              foreach (string sTempToAddress in saTo) {
                mm.To.Add(new MailAddress(sTempToAddress));
              }
            } else {
              //One To Address
              mm.To.Add(new MailAddress(toAddress, toDisplayName, System.Text.Encoding.UTF8));
            }
            #endregion

            if (!string.IsNullOrWhiteSpace(replyToAddress)) {
              mm.ReplyToList.Add(new MailAddress(replyToAddress, replyToDisplayName, System.Text.Encoding.UTF8));
            }

            if (!string.IsNullOrWhiteSpace(ccAddress)) {
              mm.CC.Add(new MailAddress(ccAddress, ccDisplayName, System.Text.Encoding.UTF8));
            }

            SendMessage(mm, sendAsync, throwOnError);
            #endregion
            break;
        }
      }
    }

    //public static void SendMessage(MailMergeMessage mmm, bool sendAsync = false, bool throwOnError = true) {
    //  try {
    //    #region Send the MailMessage (will use the Web.config settings)
    //    using (MailMergeSender mms = new MailMergeSender()) {
    //      SmtpSection smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;
    //      if (smtpSection == null) {
    //        throw new Exception("smtpSection == null");
    //      } else {
    //        //mms.EnableSsl = true; //(mm.From.Address.ToLower().Contains("@gmail.com")
    //        mms.SmtpHost = smtpSection.Network.Host;
    //        mms.SmtpPort = smtpSection.Network.Port;
    //        mms.SetSmtpAuthentification(smtpSection.Network.UserName, smtpSection.Network.Password);
    //        //mms.LocalHostName = "mail." + Environment.MachineName; 
    //        mms.MaxFailures = 1;
    //        //mms.DelayBetweenMessages = 1000;
    //        //mms.MailOutputDirectory = _outputFolder;
    //        //mms.MessageOutput = MessageOutput.Directory;  // change to MessageOutput.SmtpServer if you like, but be careful :)
    //        mms.MessageOutput = MessageOutput.SmtpServer;

    //        if (!sendAsync) {
    //          mms.Send(mmm);
    //        } else {
    //          mms.OnAfterSend += new EventHandler<MailSenderAfterSendEventArgs>(mms_OnAfterSend);
    //          // The userState can be any object that allows your callback method to identify this send operation. For this example, the userToken is a string constant.
    //          string userState = System.Guid.NewGuid().ToString();
    //          mms.SendAsync(mmm);

    //          // If the user canceled the send, and mail hasn't been sent yet, then cancel the pending operation.
    //          //if (answer.StartsWith("c") && mailSent == false) {
    //          //client.SendAsyncCancel();
    //          //}
    //        }
    //      }
    //    }
    //    #endregion
    //  } catch (Exception ex) {
    //    LogIt.E(ex);
    //    if (throwOnError) {
    //      throw;
    //    }
    //  }
    //}

    public static void SendMessageAsAsyncTask(MailMessage mm) {
      Task.Factory.StartNew(() => {
        try {
          //tried using async, but bc the calling thread went away, it was cancelled
          SendMessage(mm);
        } catch (Exception ex) {
          LogIt.E(ex);
        }
      });
    }

    public static void SendMessage(MailMessage mm, bool sendAsync = false, bool throwOnError = true) {
      try {
        #region Send the MailMessage (will use the Web.config settings)
        SmtpSection smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;
        if (smtpSection == null) {
          throw new Exception("smtpSection == null");
        } else {
          using (var sc = new System.Net.Mail.SmtpClient()) {
            sc.EnableSsl = sc.EnableSsl || (mm.From.Address.ToLower().Contains("@gmail.com"));
            //sc.Host = smtpSection.Network.Host;
            //sc.Port = smtpSection.Network.Port;
            //CredentialCache.DefaultNetworkCredentials.Password = smtpSection.Network.Password;
            //CredentialCache.DefaultNetworkCredentials.UserName = smtpSection.Network.UserName;

            if (!sendAsync) {
              sc.Send(mm);
            } else {
              // The userState can be any object that allows your callback method to identify this send operation. For this example, the userToken is a string constant.
              string userState = System.Guid.NewGuid().ToString();

              sc.SendCompleted += new SendCompletedEventHandler(mm_SendCompletedCallback);
              // The userState can be any object that allows your callback method to identify this send operation. For this example, the userToken is a string constant. 
              sc.SendAsync(mm, userState);
              //mm.Dispose();
            }
          }
        }
        #endregion
      } catch (Exception ex) {
        LogIt.E(ex);
        if (throwOnError) {
          throw;
        }
      }
    }


    private static void mm_SendCompletedCallback(object sender, AsyncCompletedEventArgs e) {
      // Get the unique identifier for this asynchronous operation.
      String token = (string)e.UserState;

      if (e.Cancelled) {
        LogIt.W("[" + token + "] Send canceled.");
      }
      if (e.Error != null) {
        LogIt.E("[" + token + "] " + e.Error.ToString());
      } else {
        //LogIt.Log("Message sent", Severity.Info);
      }
    }

    //private static void mms_OnAfterSend(object sender, MailSenderAfterSendEventArgs e) {
    //  // Get the unique identifier for this asynchronous operation.
    //  String token = ""; //(string)e.MailMergeMessage...;

    //  if (e.Cancelled) {
    //    LogIt.Log("[" + token + "] Send canceled.", Severity.Warning);
    //  }
    //  if (e.Error != null) {
    //    LogIt.Log("[" + token + "] " + e.Error.ToString(), Severity.Error);
    //  } else {
    //    //LogIt.Log("Message sent", Severity.Info);
    //  }
    //}

    //public static bool SendMessage(Chilkat.Email ce, bool sendAsync = false, bool throwOnError = true) {
    //  bool success = false;
    //  try {
    //    using (Chilkat.MailMan mm = new Chilkat.MailMan()) {
    //      using (Chilkat.Dkim dkim = new Chilkat.Dkim()) {
    //        //  The dkim object is used for creating the DKIM signature. It belongs to the "Chilkat MIME" product.
    //        if (!mm.UnlockComponent(Config.CHILKAT_EMAIL_KEY)) {
    //          LogIt.Log("Chilkat.Mail unlock key issue: " + mm.LastErrorText, Severity.Error);
    //          throw new Exception("Chilkat.Mail unlock key issue: " + mm.LastErrorText);
    //          //} else if (!dkim.UnlockComponent(Config.GetAppConfigValueAndThrowIfNullOrEmpty("CHILKAT_KEY_DKIM"))) {
    //          //LogIt.Log("Chilkat.Dkim unlock code issue: " + dkim.LastErrorText, Severity.Error);
    //          //throw new Exception("Chilkat.Dkim unlock key issue: " + dkim.LastErrorText);
    //        } else {
    //          #region Send the MailMessage (will use the Web.config settings)
    //          SmtpSection smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;
    //          if (smtpSection == null) {
    //            throw new Exception("smtpSection == null");
    //          } else {
    //            //mm.SmtpSsl = true; //(mm.From.Address.ToLower().Contains("@gmail.com")
    //            mm.SmtpHost = smtpSection.Network.Host;
    //            mm.SmtpPort = smtpSection.Network.Port;
    //            //mm.SmtpAuthMethod
    //            mm.SmtpPassword = smtpSection.Network.Password;
    //            mm.SmtpUsername = smtpSection.Network.UserName;
    //          }
    //          #endregion

    //          #region DKIM
    //          //TODO: add DKIM code, follow this, but only for the non circumvent email
    //          //http://www.example-code.com/csharp/dkim_add_signature.asp
    //          //http://www.example-code.com/csharp/dkim_sendDkimSigned.asp
    //          //http://www.example-code.com/csharp/domainKey_add_signature.asp
    //          #endregion

    //          #region send msg
    //          // Chilkat will send Hindi email encoded as utf-8, It is possible to verify this by examining the full MIME text of the email:
    //          //LogIt.Log(ce.GetMime(), Severity.Debug);

    //          // Send email...
    //          if (!sendAsync) {
    //            success = mm.SendEmail(ce);
    //            if (success) {
    //              LogIt.Log("Sent email: " + ce.GetToAddr(0), Severity.Debug);
    //            } else {
    //              LogIt.Log(mm.LastErrorText, Severity.Error);
    //            }
    //          } else {
    //            throw new NotImplementedException();
    //          }
    //          #endregion
    //        }
    //      }
    //    }
    //  } catch (Exception ex) {
    //    LogIt.E(ex);
    //    if (throwOnError) {
    //      throw;
    //    }
    //  }
    //  return success;
    //}

  }
}
