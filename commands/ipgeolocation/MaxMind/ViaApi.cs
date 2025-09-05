using System;
using System.Threading;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using MoarUtils.commands.logging;
using MoarUtils.enums;

namespace MoarUtils.commands.ipgeolocation.maxmind {
  public class ViaApi {
    public static int MAXMIND_WS_USER_ID;
    public static string MAXMIND_WS_KEY;

    private static Mutex mDr = new Mutex();

    public static InsightsResponse GetOmniResponse(string ip) {
      try {
        using (WebServiceClient wsc = new MaxMind.GeoIP2.WebServiceClient(MAXMIND_WS_USER_ID, MAXMIND_WS_KEY)) {
          var ir = wsc.Insights(ip);
          LogIt.D(ir.Country.IsoCode + "|" + ir.City.Name);
          return ir;
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        LogIt.W(ip);
        throw ex;
      }
    }

    public static string GetOrg(string ip, bool throwOnError = false) {
      try {
        using (WebServiceClient wsc = new MaxMind.GeoIP2.WebServiceClient(MAXMIND_WS_USER_ID, MAXMIND_WS_KEY)) {
          var ir = wsc.Insights(ip);
          LogIt.D(ir.Country.IsoCode + "|" + ir.City.Name);
          return ir.Traits.Organization;
        }
      } catch (Exception ex) {
        LogIt.W(ip + "|" + ex.Message);
        if (throwOnError) {
          throw ex;
        } else {
          return null;
        }
      }
    }

    public static string GetIsp(string ip, bool throwOnError = false) {
      try {
        using (WebServiceClient wsc = new MaxMind.GeoIP2.WebServiceClient(MAXMIND_WS_USER_ID, MAXMIND_WS_KEY)) {
          var ir = wsc.Insights(ip);
          LogIt.D(ir.Country.IsoCode + "|" + ir.City.Name);
          return ir.Traits.Isp;
        }
      } catch (Exception ex) {
        LogIt.W(ip + "|" + ex.Message);
        if (throwOnError) {
          throw ex;
        } else {
          return null;
        }
      }
    }

    public static string GetCountryCode(string ip, MaxmindSource mms = MaxmindSource.LocalDb, bool throwOnError = false, bool doNotWarnOnNotInDb = false) {
      try {
        using (WebServiceClient wsc = new MaxMind.GeoIP2.WebServiceClient(MAXMIND_WS_USER_ID, MAXMIND_WS_KEY)) {
          var ir = wsc.Insights(ip);
          LogIt.D(ir.Country.IsoCode + "|" + ir.City.Name);
          return ir.Country.IsoCode;
        }
      } catch (Exception ex) {
        if (doNotWarnOnNotInDb && ex.Message.Contains("is not in the database")) {
          //LogIt.W(ip + "|" + ex.Message);        
        } else {
          LogIt.W(ip + "|" + ex.Message);
        }
        if (throwOnError) {
          throw ex;
        } else {
          return null;
        }
      }
    }
  }
}
