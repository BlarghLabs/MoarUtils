using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MoarUtils.Utils {

  public class Cert {
    public static void IgnoreInvalidCerts() {
      ServicePointManager.ServerCertificateValidationCallback = TrustAllCertificatesCallback;
    }

    public static bool TrustAllCertificatesCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) {
      try {
        //LogIt.D((HttpWebRequest)sender).Address.ToString());
      } catch { }
      return true;
    }

  }
}

