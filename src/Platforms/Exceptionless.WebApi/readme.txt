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
     ASP.NET Web Api Integration
-------------------------------------
The Exceptionless.WebApi package will automatically configure your web.config.
All you need to do is open the web.config and add your Exceptionless api key to
the web.config Exceptionless section.

<exceptionless apiKey="API_KEY_HERE" />

Next, you must import the "Exceptionless" namespace and call the following line
of code to start reporting unhandled exceptions. You will need to run code during
application startup and pass it an HttpConfiguration instance. Please note that this
code is normally placed inside of the WebApiConfig classes Register method.

Exceptionless.ExceptionlessClient.Default.RegisterWebApi(config)

If you are hosting Web API inside of ASP.NET, you would register Exceptionless like:

Exceptionless.ExceptionlessClient.Default.RegisterWebApi(GlobalConfiguration.Configuration)

Please visit the documentation https://exceptionless.com/docs/clients/dotnet/sending-events/
for examples on sending events to Exceptionless.

-------------------------------------
   Manually reporting an exception
-------------------------------------
By default the Exceptionless Client will report all unhandled exceptions. You can
also manually send an exception by importing the Exceptionless namespace and calling
the following method.

exception.ToExceptionless().Submit()

Please note that Web Api doesn't have a static http context. If possible, it is recommended
that you set the HttpActionContext when submitting events. Doing so will allow the request and
user information to be populated. You can do this by calling the SetHttpActionContext EventBuilder
extension method.

exception.ToExceptionless().SetHttpActionContext(ActionContext).Submit()

-------------------------------------
      Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.