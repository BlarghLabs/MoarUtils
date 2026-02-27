using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Text;
using System.Threading;
using System.Timers;
using System.Web;
using MoarUtils.commands.exceptions;
using MoarUtils.commands.validation;
using MoarUtils.enums;
using MoarUtils.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using Twilio;

//TODO: rewite this using trace and not filehandlers

namespace MoarUtils.commands.logging {
  public sealed class LogIt {
    public enum Severity {
      Debug = 0,
      Info = 1,
      Warning = 2,
      Error = 3,
      Success = 4,
    }

    private static readonly LogIt instance = new LogIt();

    //private static bool logToFile = true;
    private Severity includeLogsAsLowAs;
    private string appEmailAddress;
    private static string logFileSubName;
    private static string logFilePath;
    private static string logFolderPath;
    private FileStream fs;
    private TextWriterTraceListener twtl;
    private ConcurrentQueue<string> al;
    private ConcurrentQueue<Action> el;
    private System.Timers.Timer flushToFile;
    private System.Timers.Timer newFileEachDay;
    private System.Timers.Timer newFileIfMaxFileSizeMet;
    private System.Timers.Timer sendEmail;
    private Mutex mutex;
    private bool emailSettingsAppearValid;
    private bool initiated = false;
    private bool inCleanup = false;
    private static string[] innerExceptionMessages = new string[] {
      "See the inner exception for details.",
      "One or more errors occurred"
    };
    private const int MAX_FLUSH_SIZE = 10;
    public static int maxFileMegaBytes = 50;
    public static bool removeNewlinesFromMessages = true;

    public static string logFolder => logFolderPath;

    public static string logFile => logFilePath;

    public static string logSubName => logFileSubName;

    #region shutdownRequested
    private static Mutex mShutdownRequested;
    private static bool _shutdownRequested;
    public static bool shutdownRequested {
      get => _shutdownRequested;
      set {
        lock (mShutdownRequested) {
          _shutdownRequested = value;
        }
      }
    }
    #endregion

    // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
    static LogIt() {
    }

    LogIt() {
      try {
        mShutdownRequested = new Mutex();
        mutex = new Mutex();
        al = new ConcurrentQueue<string>();
        el = new ConcurrentQueue<Action>();

        #region Optional Confg Values
        try {
          if (ConfigurationManager.AppSettings["LOGIT_LOG_LEVEL_FLOOR"] != null) {
            includeLogsAsLowAs = (Severity)Convert.ToInt32(ConfigurationManager.AppSettings["LOGIT_LOG_LEVEL_FLOOR"]);
          }
        } catch {
          includeLogsAsLowAs = Severity.Debug;
          al.Enqueue("error parsing LOGIT_LOG_LEVEL_FLOOR");
        }
        try {
          if (ConfigurationManager.AppSettings["LOGIT_ADMIN_EMAIL"] != null) {
            appEmailAddress = ConfigurationManager.AppSettings["LOGIT_ADMIN_EMAIL"];
          }
        } catch {
          al.Enqueue("error parsing LOGIT_ADMIN_EMAIL");
        }
        try {
          if (ConfigurationManager.AppSettings["LOGIT_SUB_NAME"] != null) {
            logFileSubName = ConfigurationManager.AppSettings["LOGIT_SUB_NAME"];
          }
        } catch {
          al.Enqueue("error parsing LOGIT_SUB_NAME");
        }
        try {
          if (ConfigurationManager.AppSettings["LOGIT_LOG_DIRECTORY"] != null) {
            logFolderPath = ConfigurationManager.AppSettings["LOGIT_LOG_DIRECTORY"];
          }
        } catch {
          al.Enqueue("error parsing LOGIT_LOG_DIRECTORY");
        }
        #endregion

        #region verify SMTP settings
        bool validSmtpSectionExists = false;
        try {
          var sne = (ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection).Network;
          validSmtpSectionExists = !string.IsNullOrEmpty(sne.Host) && !string.IsNullOrEmpty(sne.UserName);
          //can be null: && !string.IsNullOrEmpty(sne.Password) 
          //are defaulted to 25 if not supplied: && !string.IsNullOrEmpty(sne.Port.ToString());
        } catch (Exception ex) {
          al.Enqueue("WARNING: unable to parse email settings");
          al.Enqueue(ex.Message);
          al.Enqueue(ex.StackTrace);
        }
        emailSettingsAppearValid = validSmtpSectionExists
          && !string.IsNullOrEmpty(appEmailAddress)
          && EmailValidation.IsEmailValid(appEmailAddress);
        if (!emailSettingsAppearValid) {
          al.Enqueue("WARNING: will be unable to email bc email address or smtp settings were invalid");
        }
        #endregion

        #region create log file
        Trace.IndentSize = 4;
        Trace.AutoFlush = false;

        SetFilePath();
        #endregion

        InitiateTimer(ref flushToFile, 500 /* .5s */, FlushToFile);
        InitiateTimer(ref newFileEachDay, 1000 * 60 * 60 * 24 /* 24h */, CreateNewFile);
        InitiateTimer(ref newFileIfMaxFileSizeMet, 1000 * 60 * 10  /* 10m */, CreateNewFileIfMaxSize);
        InitiateTimer(ref sendEmail, 5000 /* 5s */, SendEmail);

        initiated = true;
      } catch (Exception ex) {
        Trace.WriteLine("Failed to initiate LogIt: " + ex.Message);
        throw;
      }
    }

    private static void InitiateTimer(ref System.Timers.Timer t, int interval, ElapsedEventHandler eeh) {
      t = new System.Timers.Timer {
        Interval = interval,
        Enabled = true
      };
      t.Start();
      t.Elapsed += eeh;
    }


    private void SetFilePath() {
      #region create log file dir
      if (!Directory.Exists(logFolderPath)) {
        //doesn't exist so try to create it, otherwise put in path we can write to
        //TODO: test we can write to it
        try {
          Directory.CreateDirectory(logFolderPath);
        } catch {
          logFolderPath = Path.Combine(Path.GetTempPath(), "Log");
          Directory.CreateDirectory(logFolderPath);
        }
      }

      //if this already is c:\logs\foo then it will become c:\logs\foo\foo -- fix this
      if (!string.IsNullOrEmpty(logFileSubName) && !logFolderPath.EndsWith(logFileSubName)) {
        logFolderPath = Path.Combine(logFolderPath, (string.IsNullOrEmpty(logFileSubName) ? "" : logFileSubName));
      }
      var di = Directory.CreateDirectory(logFolderPath);
      #endregion

      #region create log file
      var r = (new Random()).Next(1000);
      try {
        logFilePath = Path.Combine(logFolderPath, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "_" + DateTime.UtcNow.Ticks.ToString() + "_" + r.ToString() + ".txt");
      } catch (Exception ex0) {
        al.Enqueue(ex0.Message);
        logFilePath = Path.Combine(logFolderPath, Guid.NewGuid().ToString() + "_" + r.ToString() + ".txt");
      }
      if (fs != null) {
        fs.Close();
        fs.Dispose();
      }
      if (twtl != null) {
        twtl.Close();
        twtl.Dispose();
      }
      fs = new FileStream(logFilePath, FileMode.Create);
      twtl = new TextWriterTraceListener(fs);
      Trace.Listeners.Add(twtl);
      #endregion
    }

    ~LogIt() {
      Cleanup();
    }

    //maybe make public bc things can't delete bc this is in use?
    public void Cleanup() {
      //try to force the last of them... but... reall need this to be last-ish of app
      try {
        shutdownRequested = true;
        inCleanup = true;

        flushToFile = null;
        newFileEachDay = null;
        newFileIfMaxFileSizeMet = null;

        FlushToFile();
      } catch (Exception ex) {
        //TEMP:
        Console.Error.WriteLine(ex.Message);
      }

      //destructor cleanup statements...
      try {
        twtl.Close();
        twtl.Dispose();
      } catch (Exception ex) {
        //TEMP:
        Console.Error.WriteLine(ex.Message);
      }

      try {
        fs.Close();
        fs.Dispose();
      } catch (Exception ex) {
        //TEMP:
        Console.Error.WriteLine(ex.Message);
      }
    }

    public static LogIt Instance => instance;
    private static string GetErrorDetail(Exception ex) {
      var log = "";
      try {
        var mb = new StackFrame(1).GetMethod();
        var classAndMethod = mb.DeclaringType.Name + "|" + mb.Name;
        log = DateTime.UtcNow.ToString() + "|" + Environment.MachineName + "|" + classAndMethod + "|" + ex.Message;
      } catch (Exception ex2) {
        E(ex2);
      }
      return log;
    }

    public static void Log() {
      I("");
    }

    public static string GetMethodAndClass() {
      var mb = new StackFrame(1).GetMethod();
      var classAndMethod = mb.DeclaringType.Name + "|" + mb.Name;
      return classAndMethod;
    }

    public static void E(object o, bool fireEmailAsWell = false) {
      try {
        o = o ?? "";
        var t = o.GetType();
        if (!t.Equals(typeof(Exception)) & !typeof(Exception).IsAssignableFrom(t)) {
          Log(o, Severity.Error, fireEmailAsWell);
        } else {
          var ex = (Exception)o;
          dynamic error = new JObject();
          error.message = ex.Message;
          error.stackTrace = ex.StackTrace;

          if (ExtractIfEntityValidationErrors(ex, out string validationMessage)) {
            error.validationMessage = validationMessage;
          }
          if (ExtractIfInnerException(ex, out string innerMessage)) {
            error.innerExceptionMessage = innerMessage;
          }
          var be = GetBaseException.Execute(ex);
          if (be != null) {
            error.baseExceptionMessage = be.Message;
          }

          var json = JsonConvert.SerializeObject(error, Formatting.Indented);
          Log(json, Severity.Error, fireEmailAsWell);
        }
      } catch {
        Console.Error.WriteLine("I messed up, this all should be safe from exception");
      }
    }

    private static bool ExtractIfInnerException(Exception ex, out string message) {
      message = null;

      if (
        (ex.InnerException != null)
        &&
        !string.IsNullOrWhiteSpace(ex.InnerException.Message)
        &&
        innerExceptionMessages.Any(request => ex.Message.Contains(request))
      ) {
        var sb = new StringBuilder();
        var maxLength = 5;
        var current = ex.InnerException;
        while (maxLength-- > 0 && current != null) {
          if (!innerExceptionMessages.Any(request => current.Message.Contains(request))) {
            sb.AppendLine(current.Message);
          }
          current = current.InnerException;
        }
        message = sb.ToString();
        return true;
      }
      return false;
    }

    private static bool ExtractIfEntityValidationErrors(Exception ex, out string message) {
      var current = ex;
      message = null;

      while (current != null) {
        var validationException = current as DbEntityValidationException;
        if (validationException != null && validationException.EntityValidationErrors.Any()) {
          var sb = new StringBuilder();
          sb.AppendLine("Validation errors:");
          foreach (var entityError in validationException.EntityValidationErrors) {
            foreach (var validationError in entityError.ValidationErrors) {
              sb.Append(validationError.PropertyName).Append(": ");
              sb.Append(validationError.ErrorMessage).AppendLine(";");
            }
          }
          message = sb.ToString();
          return true;
        }
        current = current.InnerException;
      }
      return false;
    }

    public static void I(object o) {
      Log(o, Severity.Info);
    }
    public static void S(object o) {
      Log(o, Severity.Success);
    }

    public static void W(object o) {
      Log(o, Severity.Warning);
    }

    public static void D(object o) {
      Log(o, Severity.Debug);
    }

    public static void Log(object o, Severity severity, bool fireEmailAsWell = false, bool removeNewlinesFromMessage = true) {
      try {
        o = o ?? "";
        var msg = o.ToString();
        if (severity >= Instance.includeLogsAsLowAs) {
          //dont check forever
          //var methodInfo = new StackFrame(1).GetMethod();
          //if (methodInfo.DeclaringType.Name == typeof(LogIt).Name) {
          //  methodInfo = new StackFrame(2).GetMethod();
          //}
          //if (methodInfo.DeclaringType.Name == typeof(LogIt).Name) {
          //  methodInfo = new StackFrame(3).GetMethod();
          //}

          var methodInfo = new StackFrame(1).GetMethod();
          if (methodInfo.DeclaringType.Name == typeof(LogIt).Name) {
            methodInfo = new StackFrame(2).GetMethod();
          }
          if (methodInfo.DeclaringType.Name == typeof(LogIt).Name) {
            methodInfo = new StackFrame(3).GetMethod();
          }
          var className = "";
          if (methodInfo.DeclaringType != null) {
            if (methodInfo.DeclaringType.Name.StartsWith("<") || methodInfo.DeclaringType.Name.EndsWith(">")) {
              className = methodInfo.DeclaringType.ReflectedType.Name;
            } else {
              className = methodInfo.DeclaringType.Name;
            }
          }
          var method = "";
          if (methodInfo.DeclaringType != null) {
            if (methodInfo.Name.Equals("MoveNext")) {
              if (
                methodInfo.DeclaringType.Name.Contains("<")
                &&
                methodInfo.DeclaringType.Name.Contains(">")
                &&
                (methodInfo.DeclaringType.Name.IndexOf("<") < methodInfo.DeclaringType.Name.IndexOf(">"))
              ) {
                int pFrom = methodInfo.DeclaringType.Name.IndexOf("<") + "<".Length;
                int pTo = methodInfo.DeclaringType.Name.LastIndexOf(">");
                method = methodInfo.DeclaringType.Name.Substring(pFrom, pTo - pFrom);
              } else {
                method = methodInfo.DeclaringType.Name;
              }
            } else {
              method = methodInfo.Name;
            }
          }
          //switch (methodInfo.ToString()) {
          //  //I don't see current fcn
          //  case "Void LogTwimlHeaders(System.Web.HttpRequestBase)":
          //  case "Void LogHeaders(System.Web.HttpRequest)":
          //  case "Void LogHeaders(System.Web.HttpRequestBase)":
          //  case "Void Log(Twilio.RestException, Severity)":
          //  //case "Void Log(System.Exception,  MoarUtils.enums.Severity)":
          //  case "Void Log()":
          //  case "Void Log(string)":
          //  case "Void D(System.Object)":
          //  case "Void W(System.Object)":
          //  case "Void I(System.Object)":
          //  //case "Void E(System.Object)":
          //  //case "Void E(System.Exception, Boolean)":
          //  case "Void E(System.Object, Boolean)":
          //    //dig deeper
          //    methodInfo = new StackFrame(2).GetMethod();
          //    break;
          //}
          //var classAndMethod = ((methodInfo.DeclaringType == null) ? "null" : methodInfo.DeclaringType.Name) + "|" + methodInfo.Name;
          //var dt = DateTime.UtcNow;
          //var log =
          ////for format consistency
          ////https://social.msdn.microsoft.com/Forums/vstudio/en-US/bb926074-d593-4e0b-8754-7026acc607ec/datetime-tostring-colon-replaced-with-period?forum=csharpgeneral
          //dt.ToString("yyyy-MM-dd HH") + ":" + dt.ToString("mm") + ":" + dt.ToString("ss") + "." + dt.ToString("fff")
          //  + " " //this is for cloud watch logs which requires space after timestamp: http://docs.aws.amazon.com/AWSEC2/latest/WindowsGuide/send_logs_to_cwl.html
          //  + "|[" + severity.ToString().ToUpper() + "]|"
          //  + classAndMethod + "|"
          //  + (!removeNewlinesFromMessages ? msg : msg.Replace("\r\n", " ").Replace("\n", " ")); //currently just a string

          var nameSpace = "";
          if (methodInfo.DeclaringType != null) {
            nameSpace = methodInfo.DeclaringType.Namespace;
          }
          //for format consistency
          //https://social.msdn.microsoft.com/Forums/vstudio/en-US/bb926074-d593-4e0b-8754-7026acc607ec/datetime-tostring-colon-replaced-with-period?forum=csharpgeneral
          var dt = DateTime.UtcNow;
          var dts = dt.ToString("yyyy-MM-dd HH") + ":" + dt.ToString("mm") + ":" + dt.ToString("ss") + "." + dt.ToString("fff");
          //var s = Severity.Info;
          //switch (hsc) {
          //  case HttpStatusCode.OK:
          //    s = Severity.info;
          //    break;
          //  default:
          //    s = Severity.error;
          //    break;
          //}
          var ss = "[" + severity.ToString().ToUpper() + "]";
          o = o ?? "";
          //var msg = o.ToString(); //json convert instead?
          msg = !removeNewlinesFromMessage
            ? msg
            : msg.Replace("\r\n", " ").Replace("\n", " ")
          ; //currently just a string
            //var whereIAm = WhereAmI.Execute(stepUp: 3);
            //this is for cloud watch logs which requires space after timestamp: http://docs.aws.amazon.com/AWSEC2/latest/WindowsGuide/send_logs_to_cwl.html
          var space = " ";
          var log = $"{dts}{space}|{ss}|{nameSpace}|{className}|{method}|{msg}";
          Instance.al.Enqueue(log);

          if (fireEmailAsWell && Instance.emailSettingsAppearValid) {
            Instance.el.Enqueue(() => {
              Email.SendMessage(Instance.appEmailAddress, "", Instance.appEmailAddress, "", "log", log, "", "", "", "", EmailEngine.DotNet, false, true, false);
            });
          }
        }
      } catch (Exception ex) {
        throw ex;
      }
    }

    //Twilio
    //public static void Log(RestException re, Severity severity = Severity.Error) {
    //  if (re == null) {
    //    Log("rest exception was null", Severity.Error);
    //  } else {
    //    Log(re.Status + "|" + re.Code + "|" + re.Message, Severity.Error);
    //  }
    //}

    public static string LogHeaders(HttpRequest hr) {
      //this is a hack, not using hr?
      return LogHeaders(new HttpRequestWrapper(/* HttpContext.Current.Request */ hr));
    }

    public static string LogHeaders(HttpRequestBase hrb) {
      var sb = new StringBuilder();
      sb.AppendLine(hrb.Url.LocalPath + ":");
      sb.AppendLine("     " + hrb.RawUrl.ToString());
      foreach (string key in hrb.Params.Keys) {
        sb.AppendLine("     " + key + "|" + hrb[key]);
      }
      Log(sb.ToString(), Severity.Info);
      return sb.ToString();
    }

    public static void LogHeaders(HttpResponseBase hrb) {
      var sb = new StringBuilder();
      foreach (string key in hrb.Headers.Keys) {
        sb.AppendLine(key + ":" + hrb.Headers[key]);
      }
      Log(sb.ToString(), Severity.Info);
    }

    public static void LogTwimlHeaders(HttpRequestBase hrb) {
      var sb = new StringBuilder();
      sb.AppendLine(hrb.Url.LocalPath + ":");
      sb.AppendLine("     " + hrb.RawUrl.ToString());
      foreach (string key in hrb.Params.Keys) {
        switch (key.Trim()) {
          case "$Version":
          case "ALL_HTTP":
          case "ASP.NET_SessionId":
          case "ALL_RAW":
          case "APPL_MD_PATH":
          case "APPL_PHYSICAL_PATH":
          case "AUTH_TYPE":
          case "AUTH_USER":
          case "AUTH_PASSWORD":
          case "LOGON_USER":
          case "REMOTE_USER":
          case "CERT_COOKIE":
          case "CERT_FLAGS":
          case "CERT_ISSUER":
          case "CERT_KEYSIZE":
          case "CERT_SECRETKEYSIZE":
          case "CERT_SERIALNUMBER":
          case "CERT_SERVER_ISSUER":
          case "CERT_SERVER_SUBJECT":
          case "CERT_SUBJECT":
          case "CONTENT_LENGTH":
          case "CONTENT_TYPE":
          case "GATEWAY_INTERFACE":
          case "HTTPS":
          case "HTTPS_KEYSIZE":
          case "HTTPS_SECRETKEYSIZE":
          case "HTTPS_SERVER_ISSUER":
          case "HTTPS_SERVER_SUBJECT":
          case "INSTANCE_ID":
          case "INSTANCE_META_PATH":
          case "LOCAL_ADDR":
          case "PATH_INFO":
          case "PATH_TRANSLATED":
          case "REMOTE_ADDR":
          case "REQUEST_METHOD":
          case "SCRIPT_NAME":
          case "SERVER_NAME":
          case "SERVER_PORT":
          case "SERVER_PORT_SECURE":
          case "SERVER_PROTOCOL":
          case "SERVER_SOFTWARE":
          case "URL":
          case "HTTP_CACHE_CONTROL":
          case "HTTP_CONNECTION":
          case "HTTP_COOKIE":
          case "HTTP_HOST":
          case "HTTP_USER_AGENT":
          case "HTTP_X_TWILIO_SIGNATURE":
          case "HTTP_X_TWILIO_SSL":
            //ignore these
            break;
          default:
            sb.AppendLine("     " + key + "|" + hrb[key]);
            break;
        }
      }
      Log(sb.ToString(), Severity.Info);
    }

    public /* private */ static void FlushToFile(object sender = null, ElapsedEventArgs e = null) {
      try {
        if (!shutdownRequested) {
          if (Instance.initiated) {
            lock (Instance.mutex) {
              var alBuffer = new List<string>();
              string bufferItem;
              while (Instance.al.TryDequeue(out bufferItem)) {
                alBuffer.Add(bufferItem);
              }

              //grab fixed number of events and log just those, not any added after we got here
              var numToPop = alBuffer.Count < MAX_FLUSH_SIZE ? alBuffer.Count : MAX_FLUSH_SIZE;

              for (int i = 0; i < numToPop; i++) {
                Trace.WriteLine(alBuffer[i]);
                #region during cleanup, Trace breaks, so we append explicitly
                if (Instance.inCleanup) {
                  using (var sw = File.AppendText(logFilePath)) {
                    sw.WriteLine(alBuffer[i]);
                  }
                }
                #endregion
                if (alBuffer[i].ToString().Contains("[ERROR]")) {
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.Error.WriteLine(alBuffer[i]); //maybe do error.writeline if we see [error]
                } else if (alBuffer[i].ToString().Contains("[WARNING]")) {
                  Console.ForegroundColor = ConsoleColor.Yellow;
                  Console.Error.WriteLine(alBuffer[i]); //maybe do error.writeline if we see [error]
                } else if (alBuffer[i].ToString().Contains("[SUCCESS]")) {
                  Console.ForegroundColor = ConsoleColor.Green;
                  Console.WriteLine(alBuffer[i]); //maybe do error.writeline if we see [error]
                } else {
                  Console.ForegroundColor = ConsoleColor.White;
                  Console.WriteLine(alBuffer[i]); //maybe do error.writeline if we see [error]
                }
              }
              Trace.Flush();
              //fs.Close();         
            }
          }
        }
      } catch { //(Exception ex) {
        //throw ex;
      }
    }


    private static void CreateNewFileIfMaxSize(object sender = null, ElapsedEventArgs e = null) {
      //Create new file if > max file size 
      if (Instance.initiated) {
        if (!shutdownRequested) {
          lock (Instance.mutex) {
            var fi = new FileInfo(logFilePath);
            var maxBytes = maxFileMegaBytes * 1000 * 1000;
            if (fi.Length > (maxBytes)) {
              Instance.SetFilePath();
            }
          }
        }
      }
    }

    private static void CreateNewFile(object sender = null, ElapsedEventArgs e = null) {
      //Create new file if we have been open for over 24 hours
      if (Instance.initiated) {
        if (!shutdownRequested) {
          lock (Instance.mutex) {
            Instance.SetFilePath();
          }
        }
      }
    }

    private static void SendEmail(object sender = null, ElapsedEventArgs e = null) {
      try {
        Action action;
        while (Instance.el.TryDequeue(out action)) {
          action();
        }
      } catch (Exception ex) {
        Log(ex, Severity.Error);
      }
    }
  }
}
