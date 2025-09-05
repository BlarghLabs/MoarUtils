using System;
using System.Collections.Generic;
using System.Threading;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Model;
using MaxMind.GeoIP2.Responses;
using MoarUtils.commands.logging;
namespace MoarUtils.commands.ipgeolocation.maxmind {
  public class ViaDb {
    public static int MAXMIND_WS_USER_ID;
    public static string MAXMIND_WS_KEY;
    //requires these files to be accessable and present
    public static string MAXMIND_CITY_DB_PATH = @"c:\maxmindfiles\GeoIP2-City.mmdb"; // //GeoLite2-City.mmdb
    public static string MAXMIND_ISP_DB_PATH = @"c:\maxmindfiles\GeoIP2-ISP.mmdb";

    private static Mutex mDr = new Mutex();
    private static DatabaseReader _dr = null;
    public static DatabaseReader maxmindDr {
      get {
        if (_dr == null) {
          lock (mDr) {
            if (_dr == null) {
              _dr = new MaxMind.GeoIP2.DatabaseReader(MAXMIND_CITY_DB_PATH, MaxMind.Db.FileAccessMode.Memory);
            }
          }
        }
        return _dr;
      }
    }

    private static Mutex mIspDr = new Mutex();
    private static DatabaseReader _isPdr = null;
    public static DatabaseReader maxmindIspDr {
      get {
        if (_isPdr == null) {
          lock (mIspDr) {
            if (_isPdr == null) {
              _isPdr = new MaxMind.GeoIP2.DatabaseReader(MAXMIND_ISP_DB_PATH, MaxMind.Db.FileAccessMode.Memory);
            }
          }
        }
        return _isPdr;
      }
    }

    public static string GetLocation(string ip, bool throwOnError = false, bool warnOnNotInDb = false) {
      string result = "";
      try {
        var cr = maxmindDr.City(ip);
        var r = maxmindIspDr.Isp(ip);
        result = string.Join(",",
          new List<object>{
            (cr == null) ? null : cr.City.Name,
            (cr == null) ? null : cr.Country.IsoCode,
            (r == null) ? null : r.Isp,
            (r == null) ? null : r.Organization
          });
        return result;
      } catch (Exception ex) {
        if (!warnOnNotInDb && ex.Message.Contains("is not in the database")) {
          //LogIt.W(ip + "|" + ex.Message);        
        } else {
          LogIt.W(ip + "|" + ex.Message);
        }
        if (throwOnError) {
          throw ex;
        } else {
          return null;
        }
      } finally {
        LogIt.D(result);
      }
    }

    public static City GetCity(string ip) {
      try {
        var cr = maxmindDr.City(ip);
        //LogIt.D(cr.Country.IsoCode + "|" + cr.City.Name);
        return cr.City;
      } catch (Exception ex) {
        LogIt.E(ex);
        LogIt.W(ip);
        throw ex;
      }
    }


    public static string GetOrg(string ip, bool throwOnError = false) {
      try {
        var r = maxmindIspDr.Isp(ip);
        return r.Organization;
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
        var r = maxmindIspDr.Isp(ip);
        return r.Isp;
      } catch (Exception ex) {
        LogIt.W(ip + "|" + ex.Message);
        if (throwOnError) {
          throw ex;
        } else {
          return null;
        }
      }
    }

    public static string GetCountryName(
      string ip,
      bool throwOnError = false,
      bool doNotWarnOnNotInDb = true
    ) {
      try {
        var cr = maxmindDr.City(ip);
        //LogIt.D(or.Country.IsoCode + "|" + or.City.Name);
        return cr.Country.Name;
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

    public static string GetCountryCode(string ip, bool throwOnError = false, bool doNotWarnOnNotInDb = true) {
      try {
        var cr = maxmindDr.City(ip);
        //LogIt.D(or.Country.IsoCode + "|" + or.City.Name);
        return cr.Country.IsoCode;
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
    public static CityResponse GetCityResponse(
      string ip,
      bool throwOnError = false,
      bool doNotWarnOnNotInDb = true
    ) {
      try {
        if (string.IsNullOrWhiteSpace(ip)) {
          return null;
        }
        if (ip == "::1") {
          return null;
        }
        var cr = maxmindDr.City(ip);
        return cr;
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
