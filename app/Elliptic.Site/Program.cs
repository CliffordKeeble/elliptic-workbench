using Elliptic.Site;

RepositorySite site;
try
{
    site = RepositorySite.Locate(AppContext.BaseDirectory);
}
catch (SiteNotServableException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapRepositorySite(site);

app.Lifetime.ApplicationStarted.Register(() => StartupLog.Serving(app.Logger, site.Root, app.Urls));

app.Run();
return 0;

// Source-generated, not LogInformation: the message is not built, and the arguments not formatted,
// unless Information is enabled. The urls go in as the collection; the logger joins them with ", ".
internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Serving {Folder} at {Urls}")]
    public static partial void Serving(ILogger logger, string folder, IEnumerable<string> urls);
}

// Lets the test project name this assembly's entry point for WebApplicationFactory.
public partial class Program;
