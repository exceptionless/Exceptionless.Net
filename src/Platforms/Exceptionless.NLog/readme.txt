-------------------------------------
		Exceptionless Readme
-------------------------------------
Exceptionless provides real-time error reporting for your apps. It organizes the
gathered information into simple actionable data that will help your app become
exceptionless!

Learn more at http://exceptionless.io.

-------------------------------------
		How to get an api key
-------------------------------------
The Exceptionless client requires an api key to use the Exceptionless service.
You can get your Exceptionless api key by logging into http://exceptionless.io
and viewing your project configuration page.

-------------------------------------
		General Data Protection Regulation
-------------------------------------
By default the Exceptionless Client will report all available metadata including potential PII data.
You can fine tune the collection of information via Data Exclusions or turning off collection completely.

Please visit the documentation https://exceptionless.com/docs/clients/dotnet/private-information/
for detailed information on how to configure the client to meet your requirements.

-------------------------------------
		 NLog Integration
-------------------------------------

Here is an example NLog.config file that shows how to use the Exceptionless NLog target. The apiKey attribute
is optional and will be picked up from your Exceptionless config section by default. It is recommended to set
the minLevel on the Exceptionless target to Trace so that you can control log levels in the server side
client configuration settings. Also, you can call Configuration.SetDefaultMinLogLevel to control the default
minimum log level that will be used until the client retrieves settings from the server.

<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <extensions>
    <add assembly="Exceptionless.NLog"/>
  </extensions>

  <targets async="true">
    <target xsi:type="Exceptionless" name="exceptionless" apiKey="API_KEY_HERE">
      <field name="host" layout="${machinename}" />
      <field name="identity" layout="${identity}" />
      <field name="windows-identity" layout="${windows-identity:userName=True:domain=False}" />
      <field name="process" layout="${processname}" />
    </target>
  </targets>

  <rules>
    <logger name="*" minlevel="Trace" writeTo="exceptionless" />
  </rules>
</nlog>

-------------------------------------
	  Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.
