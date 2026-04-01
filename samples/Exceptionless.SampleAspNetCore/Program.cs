using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// By default sends warning and error log messages to Exceptionless.
// Log levels can be controlled remotely per log source from the Exceptionless app in near real-time.
builder.Logging.AddExceptionless();

// Reads settings from IConfiguration then adds additional configuration from this lambda.
// This also configures ExceptionlessClient.Default and host shutdown queue flushing.
builder.AddExceptionless(c => c.DefaultData["Startup"] = "heyyy");
// OR
// builder.AddExceptionless();
// OR
// builder.AddExceptionless("API_KEY_HERE");

// Adds ASP.NET Core request/unhandled exception hooks and standard exception handling services.
builder.Services.AddExceptionlessExceptionHandler();
builder.Services.AddProblemDetails();

// This is normal ASP.NET Core code.
builder.Services.AddControllers();

var app = builder.Build();

// Uses the built-in exception handler pipeline, with Exceptionless capturing via IExceptionHandler.
app.UseExceptionHandler();

// Adds Exceptionless middleware for diagnostics, 404 tracking, and queue processing.
app.UseExceptionless();

app.MapControllers();

app.Run();
