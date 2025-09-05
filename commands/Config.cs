using System;
using System.Configuration;
using MoarUtils.commands.logging;
using static MoarUtils.commands.logging.LogIt;

namespace MoarUtils.Utils {

  public sealed class Config {
    //static readonly Config instance = new Config();
    static Config instance = new Config();

    // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
    static Config() {
    }

    Config() {
      try {
        //those required
        //CHILKAT_EMAIL_KEY = GetRequiredConfigValue("CHILKAT_EMAIL_KEY");

        //those not required: 
        CHILKAT_EMAIL_KEY = ConfigurationManager.AppSettings["CHILKAT_EMAIL_KEY"];
      } catch (Exception ex) {
        LogIt.E(ex);
        throw new Exception("Config error");
      }
    }

    public static Config Instance {
      get {
        return instance;
      }
    }

    public static string CHILKAT_EMAIL_KEY;

    private static string GetRequiredConfigValue(string key) {
      var value = "";
      try {
        value = ConfigurationManager.AppSettings[key];
        if (String.IsNullOrWhiteSpace(value)) {
          LogIt.Log("Config value was null: " + key, Severity.Error);
          throw new Exception("Config value was null: " + key);
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        throw; //?
      }
      return value;
    }
  }
}
