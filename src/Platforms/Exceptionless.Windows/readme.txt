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
		 Windows Integration
-------------------------------------
If your project has an app.config file, the Exceptionless.Windows NuGet package
will automatically configure your app.config with the required config sections.
All you need to do is open the app.config and add your Exceptionless api key to
the app.config Exceptionless section.

<exceptionless apiKey="API_KEY_HERE" />

If your project does not have an app.config file, then please add the following
assembly attribute and your own Exceptionless api key to your project (E.G., AssemblyInfo class).

[assembly: Exceptionless.Configuration.Exceptionless("API_KEY_HERE")]

Finally, you must import the Exceptionless namespace and call the following line
of code to start reporting unhandled exceptions.

Exceptionless.ExceptionlessClient.Default.Register()

Please visit the documentation https://exceptionless.com/docs/clients/dotnet/sending-events/
for examples on sending events to Exceptionless.

-------------------------------------
   Manually reporting an exception
-------------------------------------
By default the Exceptionless Client will report all unhandled exceptions. You can
also manually send an exception by importing the Exceptionless namespace and calling
the following method.

exception.ToExceptionless().Submit()

-------------------------------------
   Session Tracking
-------------------------------------
Exceptionless can also track user sessions which enables powerful application analytics.

Session tracking can be enabled by simply adding this line to the startup of your application:

ExceptionlessClient.Default.Configuration.UseSessions()

You will also need to tell Exceptionless who the current user is in your application when the user logs in:

ExceptionlessClient.Default.Configuration.SetUserIdentity("UNIQUE_ID_OR_EMAIL_ADDRESS", "Display Name")

-------------------------------------
	  Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.
