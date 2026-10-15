using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Elliptic.Site;

public static class SiteEndpoints
{
    /// <summary>
    /// Serves the pages from the repository folders themselves, not from a copy.
    ///
    /// Why not link them into wwwroot: (1) an edit to a page shows on refresh, with no rebuild
    /// and no copy step to forget; (2) the linked-into-wwwroot route was measured failing: under
    /// "dotnet run" in Development the static web assets loader throws DirectoryNotFoundException
    /// for app/Elliptic.Site/wwwroot/, because no such folder exists beside the project.
    ///
    /// Why not point the web root at the repository and call a bare UseStaticFiles(): that would
    /// serve src/, tools/ and .git. Only the list in <see cref="RepositorySite"/> is mounted.
    /// </summary>
    public static void MapRepositorySite(this WebApplication app, RepositorySite site)
    {
        // The web/ folder, whole: pages, fonts and their licences.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(site.WebFolder),
            RequestPath = "/" + RepositorySite.WebFolderName,
        });

        // Routing comes after the static files on purpose. The static files middleware stands
        // aside once an endpoint has been selected, and the catch-all below would otherwise
        // be selected for every /web/... path and shadow the folder.
        app.UseRouting();

        var contentTypes = new FileExtensionContentTypeProvider();

        foreach (var file in RepositorySite.RootFiles)
        {
            var path = site.PathOf(file);
            app.MapGet("/" + file, (HttpContext context) =>
                SendFile(context, path, StatusCodes.Status200OK, contentTypes));
        }

        var home = site.PathOf(RepositorySite.HomePage);
        app.MapGet("/", (HttpContext context) =>
            SendFile(context, home, StatusCodes.Status200OK, contentTypes));

        // Anything else: 404 carrying the body of 404.html, as the deployed site does. The
        // explicit pattern matters: MapFallback's default skips any path with a file extension,
        // so "/README.md" would get an empty 404 and not the page.
        var notFound = site.PathOf(RepositorySite.NotFoundPage);
        app.MapFallback("{*path}", (HttpContext context) =>
            SendFile(context, notFound, StatusCodes.Status404NotFound, contentTypes));
    }

    private static async Task SendFile(
        HttpContext context, string path, int status, FileExtensionContentTypeProvider contentTypes)
    {
        contentTypes.TryGetContentType(path, out var contentType);
        context.Response.StatusCode = status;
        context.Response.ContentType = contentType ?? "application/octet-stream";
        await context.Response.SendFileAsync(path);
    }
}
