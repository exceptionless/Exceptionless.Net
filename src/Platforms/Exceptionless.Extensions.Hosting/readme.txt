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
Microsoft.Extensions.Hosting Integration
-------------------------------------
You must import the "Exceptionless" namespace and register Exceptionless on the
host builder.

var builder = Host.CreateApplicationBuilder(args);
builder.AddExceptionless(c => c.ApiKey = "API_KEY_HERE");
builder.UseExceptionless();

`AddExceptionless(...)` configures the client, and `UseExceptionless()` ensures
the pending queue is flushed during host shutdown.

Please visit the documentation https://exceptionless.com/docs/clients/dotnet/sending-events/
for examples on sending events to Exceptionless.

-------------------------------------
      Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.
