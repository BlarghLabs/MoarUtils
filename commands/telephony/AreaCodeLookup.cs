using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MoarUtils.commands.logging;
using MoarUtils.models.commands;
using PhoneNumbers;
namespace MoarUtils.Utils.Telephony {
  public class AreaCodeLookup {
    #region Local List
    private static Mutex request = new Mutex();
    private static List<AreaCode> _loac = null;
    public static List<AreaCode> loac {
      get {
        lock (request) {
          //if we have some and then are <3days old, then return them
          if (_loac == null) {
            //else, get new
            _loac = AcquireFromResources();
          }
          return _loac;
        }
      }
    }

    /// <summary>
    /// Only US and CA
    /// </summary>
    public static List<AreaCode> loacUsCa {
      get {
        return loac.Where(ac => (ac.stateCode.Length == 2)).ToList();
      }
    }
    #endregion

    public static string GetStateCode(string areaCode) {
      try {
        return loacUsCa.Where(ac => ac.numberCode == areaCode).Select(ac => ac.stateCode).FirstOrDefault();
      } catch (Exception ex) {
        LogIt.E(ex);
        return null;
      }
    }
    //this does not return state name... wonder why ic ommited
    public static string GetStateName(string areaCode) {
      try {
        return loacUsCa.Where(ac => ac.numberCode == areaCode).Select(ac => ac.stateCode).FirstOrDefault();
      } catch (Exception ex) {
        LogIt.E(ex);
        return null;
      }
    }


    public static List<AreaCode> AcquireFromResources() {
      try {
        var content = Properties.Resources.area_codes;
        var lol = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        //skip header: Area Code,Region
        lol.RemoveAt(0);
        _loac = new List<AreaCode> { };
        foreach (var l in lol) {
          var los = l.Split(new char[] { ',' });
          //var location = los[1];
          //if (location.Length == 2) {
          //  if (States.GetStateName(location) != null) {
          //    location = States.GetStateName(location);
          //  }
          //}
          //do I include non us/ca states?
          _loac.Add(new AreaCode { numberCode = los[0], stateCode = los[1] });
        }

        return _loac;
      } catch (Exception ex) {
        LogIt.E(ex);
        return null;
      }
    }

    public static string GetAreaCode(PhoneNumber pn) {
      var pnu = PhoneNumberUtil.GetInstance();
      var nationalSignificantNumber = pnu.GetNationalSignificantNumber(pn);
      String areaCode;
      String subscriberNumber;
      int areaCodeLength = pnu.GetLengthOfGeographicalAreaCode(pn);
      if (areaCodeLength > 0) {
        areaCode = nationalSignificantNumber.Substring(0, areaCodeLength);
        subscriberNumber = nationalSignificantNumber.Substring(areaCodeLength);
      } else {
        areaCode = "";
        subscriberNumber = nationalSignificantNumber;
      }
      return areaCode;
    }
  }
}


////https://raw.githubusercontent.com/kedarmhaswade/cities/master/area-codes.csv
////https://raw.githubusercontent.com/yangsu/hackduke/master/sampleapps/areacode.json
//public static Hashtable AcquireAreaCodesFromWeb() {
//  try {
//    var response = new RestClient {
//      BaseUrl = new Uri("https://raw.githubusercontent.com"),
//      Timeout = 10000,
//      UserAgent = "AreaCodeLookup"
//    }.Execute(new RestRequest {
//      //Resource = "yangsu/hackduke/master/sampleapps/areacode.json",
//      Resource = "kedarmhaswade/cities/master/area-codes.csv",
//      Method = Method.Get,
//      //RequestFormat = RestSharp.DataFormat.Json
//    });
//    var content = response.Content;

//    var lol = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
//    //skip header: Area Code,Region
//    lol.RemoveAt(0);
//    _ht = new Hashtable();
//    foreach (var l in lol) {
//      var los = l.Split(new char[] { ',' });
//      var location = los[1];
//      if (location.Length == 2) {
//        if (States.GetName(location) != null) {
//          location = States.GetName(location);
//        }
//      }
//      _ht.Add(los[0], location);
//    }

//    return _ht;
//  } catch (Exception ex) {
//    LogIt.E(ex);
//    return null;
//  }
//}