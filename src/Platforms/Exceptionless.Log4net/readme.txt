﻿-------------------------------------
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
		 log4net Integration
-------------------------------------

Here is an example log.config file that shows how to use the Exceptionless log4net appender.

<appender name="exceptionless" type="Exceptionless.Log4net.ExceptionlessAppender,Exceptionless.Log4net" />

By default, the appender will use the settings from the Exceptionless config section, but
you can also set it on the appender like this:

<appender name="exceptionless" type="Exceptionless.Log4net.ExceptionlessAppender, Exceptionless.Log4net">
    <apiKey value="API_KEY_HERE" />
</appender>
-------------------------------------
	  Documentation and Support
-------------------------------------
Please visit http://exceptionless.io for documentation and support.