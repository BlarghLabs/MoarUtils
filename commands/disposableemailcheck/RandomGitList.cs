using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MoarUtils.commands.logging;
using Newtonsoft.Json.Linq;
using RestSharp;
namespace MoarUtils.Utils.DisposableEmailCheck {
  public class RandomGitList {
    #region Local List
    private static Mutex mLoDomains = new Mutex();
    private static DateTime lastAggregatedDomainList = DateTime.MinValue;
    private static List<string> _loDomains = null;
    public static List<string> loDomains {
      get {
        lock (mLoDomains) {
          //if we have some and then are <2 min old, then return them
          if ((_loDomains == null) || (lastAggregatedDomainList < DateTime.UtcNow.AddHours(-12))) {
            //else, get new
            _loDomains = AcquireDomainList();
            lastAggregatedDomainList = DateTime.UtcNow;
            LogIt.D("total:" + _loDomains.Count + ((_loDomains.Count == 0) ? "" : ("|first:" + _loDomains[0] + "|last:" + _loDomains[_loDomains.Count - 1])));
          }
          return _loDomains;
        }
      }
    }
    #endregion

    public static bool IsDisposable(string email) {
      try {
        var disposableDomain = loDomains.Where(d => email.EndsWith(d, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
        return (disposableDomain != null);
      } catch (Exception ex) {
        LogIt.E(ex);
        return false;
      }
    }

    public static List<string> AcquireDomainList() {
      try {
        var client = new RestClient(new RestClientOptions {
          BaseUrl = new Uri("https://raw.githubusercontent.com/"),
          //Timeout = 10000,
          MaxTimeout = 10000,
          UserAgent = "DisposableEmailCheck"
        });
        var response = client.ExecuteAsync(new RestRequest {
          Resource = "ivolo/disposable-email-domains/master/index.json",
          Method = Method.Get,
          //RequestFormat = RestSharp.DataFormat.Json
        }).Result;
        var content = response.Content;
        content = "{\"List\":" + content + "}";
        dynamic json = Newtonsoft.Json.Linq.JObject.Parse(content);
        var jArray = json.List as JArray;
        var los = jArray.Select(s => Convert.ToString(s)).ToList();

        //      [
        //"0-mail.com",
        //"0815.ru",
        //"0clickemail.com",
        //"0wnd.net",
        //"0wnd.org",
        //"10minutemail.com",
        //"20minutemail.com",
        //"2prong.com",
        //"30minutemail.com",
        //"33mail.com",
        //"3d-painting.com",
        //"4warding.com",
        //"4warding.net",
        //"4warding.org",
        //"60minutemail.com",
        //"675hosting.com",
        //"675hosting.net",
        //"675hosting.org",

        //arcor.de - Web-based email, NOT disposable
        //bk.ru - Web-based email (mail.ru alias), NOT disposable
        //gawab.com - Very well-known, spam-friendly Web-based email, NOT disposable
        //highendsoul.com - spammer domain, NOT disposable
        //highschoolindex.com - spammer domain, NOT disposable
        //kinozal.tv - Web-based email, NOT disposable
        //list.ru - Web-based email, NOT disposable
        //mainru.com - Web-based email, NOT disposable
        //o2.pl - Web-based email, NOT disposable
        //thecharitylink.com - spammer domain, NOT disposable
        //yandex.ru - Web-based email, NOT disposable

        var loClean = new List<string> { "qq.com", "bk.ru", "arcor.de", "gawab.com", "highendsoul.com", "highschoolindex.com", "kinozal.tv", "list.ru", "mainru.com", "o2.pl", "thecharitylink.com", "yandex.ru" };
        foreach (var c in loClean) {
          if (los.Contains(c)) {
            los.Remove(c);
          }
        }

        if (!los.Contains("wp.pl")) {
          los.Add("wp.pl");
        }

        return los;
      } catch (Exception ex) {
        LogIt.E(ex);
        return new List<string>();
      }
    }
  }
}


