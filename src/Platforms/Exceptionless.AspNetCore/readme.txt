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

Please visit the wiki https://github.com/exceptionless/Exceptionless.Net/wiki/Configuration#general-data-protection-regulation
for detailed information on how to configure the client to meet your requirements.

-------------------------------------
      ASP.NET Core Integration
-------------------------------------
You must import the "Exceptionless" namespace and call the following line
of code to start reporting unhandled exceptions. The best place to call this
code is at the first line of the Configure method inside of the Startup class.

app.UseExceptionless("API_KEY_HERE");

Alternatively, you can also use the different overloads of the UseExceptionless method
for different configuration options.

Please visit the wiki https://github.com/exceptionless/Exceptionless.Net/wiki/Sending-Events
for examples on sending events to Exceptionless.

-------------------------------------
   Manually reporting an exception
-------------------------------------
By default the Exceptionless Client will report all unhandled exceptions. You can
also manually send an exception by importing the Exceptionless namespace and calling
the following method.

exception.ToExceptionless().Submit()

Please note that ASP.NET Core doesn't have a static http context. We recommend registering
the http context accessor. Doing so will allow the request and user information to be populated. 
You can do this by calling the AddHttpContextAccessor while configure services.

services.AddHttpContextAccessor()

-------------------------------------
      Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.