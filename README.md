
[![NuGet version (MoarUtils)](https://img.shields.io/nuget/v/MoarUtils.svg)](https://www.nuget.org/packages/MoarUtils/)

[![Build Status](https://sobelito.visualstudio.com/MoarUtils/_apis/build/status/BlarghLabs.MoarUtils?branchName=master)](https://sobelito.visualstudio.com/MoarUtils/_build/latest?definitionId=2&branchName=master)

Optional App/Web.Config values:

```xml
<!-- LogIt -->
<appSettings>
  <add key="LOGIT_LOG_LEVEL_FLOOR" value="0"/>
  <!-- 0/Debug, 1/Info,2/Warning,3/Error -->
  <add key="LOGIT_SUB_NAME" value="myapp"/>
  <!-- prefix added to log file name -->
  <add key="LOGIT_LOG_DIRECTORY" value="c:\logs"/>
  <!-- if supplied then tries to put logs here, otherwise are placed in C:\Users\USERX\AppData\Local\BlarghLabs\MoarUtils\Log -->
  <add key="LOGIT_ADMIN_EMAIL" value="foo@bar.baz"/>
  <!-- if valid system.net email provider is also supplied will use email functionality and send approp alerts to this email -->
</appSettings>

<!-- Email -->
<appSettings>
  <add key="CHILKAT_EMAIL_KEY" value="ENTER_PURCHASED_KEY_HERE"/>
  <!-- Value achived via Chilkat purchase -->
</appSettings>
<system.net>
  <mailSettings>
    <smtp>
      <network host="MAIL_SERVER_IP_OR_HOST" password="PASSWPRD_HERE" userName="USERNAME_HERE" port="SMTP_PORT_HERE"/>
    </smtp>
  </mailSettings>
</system.net>
```
