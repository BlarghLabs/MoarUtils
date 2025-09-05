using System;
using System.Web.UI.HtmlControls;

namespace MoarUtils.Utils {
  public class BootstrapUtils {
    public enum BootstrapAlertFormat { success, info, warning, danger };
    public static void SetStatus(string msg, BootstrapAlertFormat baf, ref HtmlGenericControl div) {
      div.Attributes["class"] = "alert alert-" + baf.ToString();
      div.Visible = !string.IsNullOrWhiteSpace(msg);
      div.InnerHtml = msg;
    }
    public static void SetStatus(Exception ex, ref HtmlGenericControl div) {
      SetStatus(ex.Message, BootstrapAlertFormat.danger, ref div);
    }

  }
}
