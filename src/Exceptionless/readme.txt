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
		 Integration
-------------------------------------
This library is platform agnostic and is compiled against different runtimes. Depending on the 
referenced runtime, Exceptionless will attempt to wire up to available error handlers and attempt to
discover configuration settings available to that runtime. For these reasons if you are on a known 
platform then use the platform specific package to save you time configuring while giving you more 
contextual information. For more information and configuration examples please read the Exceptionless 
Configuration documentation on http://docs.exceptionless.io/contents/configuration/

On app startup, import the Exceptionless namespace and call the client.Startup() extension method
to wire up to any runtime specific error handlers and read any available configuration.

using Exceptionless;
ExceptionlessClient.Default.Startup("API_KEY_HERE");

Please visit the wiki https://github.com/exceptionless/Exceptionless.Net/wiki/Sending-Events
for examples on sending events to Exceptionless.

-------------------------------------
   Manually reporting an exception
-------------------------------------
By default the Exceptionless Client will report all unhandled exceptions. You can 
also manually send an exception by importing the Exceptionless namespace and calling 
the following method.

exception.ToExceptionless().Submit()

-------------------------------------
	  Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.
