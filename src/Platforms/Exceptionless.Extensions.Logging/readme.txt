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
Microsoft.Extensions.Logging Integration
-------------------------------------
You must import the "Exceptionless" namespace and call the following line
of code to start reporting log messages.

loggerFactory.AddExceptionless("API_KEY_HERE");

Alternatively, you can also use the different overloads of the AddExceptionless method
for different configuration options.

Please visit the wiki https://github.com/exceptionless/Exceptionless.Net/wiki/Sending-Events
for examples on sending events to Exceptionless.

-------------------------------------
      Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.