using Elliptic.Site;

RepositorySite site;
try
{
    site = RepositorySite.Locate(AppContext.BaseDirectory);
}
catch (RepositoryNotFoundException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapRepositorySite(site);

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Serving {Folder} at {Urls}", site.Root, string.Join(", ", app.Urls)));

app.Run();
return 0;

// Lets the test project name this assembly's entry point for WebApplicationFactory.
public partial class Program;
