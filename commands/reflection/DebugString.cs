using System;
using System.Collections.Generic;
using MoarUtils.commands.logging;
using Newtonsoft.Json;

namespace MoarUtils.Utils {
  public class DebugString {
    public static string GetJson(object o, Formatting f = Formatting.None) {
      var json = "";
      try {
        json = Newtonsoft.Json.JsonConvert.SerializeObject(o, f);
      } catch (Exception ex) {
        LogIt.E(ex);
      } finally {
      }
      return json;
    }

    //TODO: fix this so it works w/ collection
    public static string Get(object o, bool onlyPublic = true, string delimiter = "|") {
      var result = "";
      var loo = new List<object> { };
      try {
        if (o == null) {
          loo.Add("null");
        } else if (o is string) {
          loo.Add(o);
        } else {
          var pia = o.GetType().GetProperties();
          foreach (var pi in pia) {
            if (!onlyPublic || (onlyPublic && pi.PropertyType.IsPublic)) {
              loo.Add(
                pi.Name.ToString().Trim() + ":" + ((pi.GetValue(o) == null) ? "" : pi.GetValue(o).ToString().Trim())
              );
            }
          }

          var fia = o.GetType().GetFields();
          foreach (var fi in fia) {
            if (!onlyPublic || (onlyPublic && fi.IsPublic)) {
              loo.Add(
                fi.Name.ToString().Trim() + ":" + ((fi.GetValue(o) == null) ? "" : fi.GetValue(o).ToString().Trim())
              );
            }
          }
        }
      } catch (Exception ex) {
        LogIt.E(ex);
      } finally {
        try {
          result = string.Join(delimiter, loo);
        } catch (Exception ex1) {
          LogIt.E(ex1);
        }
      }
      return result;
    }
  }
}
