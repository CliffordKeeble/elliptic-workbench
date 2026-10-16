using System.Net;
using Elliptic.Site;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Elliptic.Site.Tests;

public sealed class SiteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly RepositorySite _repo = RepositorySite.Locate(AppContext.BaseDirectory);

    public SiteTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// The input list of tools/build-site.sh, read from the script and not copied here, so a page
    /// added to the deploy and not to this host goes red. _headers is Cloudflare's header file,
    /// not a page.
    /// </summary>
    private IEnumerable<string> PublishedInputs()
    {
        var script = File.ReadAllText(_repo.PathOf("tools/build-site.sh")).Replace("\r", "");
        var joined = script.Replace("\\\n", " ");
        var line = joined.Split('\n').Single(l => l.TrimStart().StartsWith("for f in ", StringComparison.Ordinal));
        var list = line.TrimStart()["for f in ".Length..];
        list = list[..list.IndexOf("; do", StringComparison.Ordinal)];
        var inputs = list.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(inputs); // a parse that finds nothing must not pass vacuously
        return inputs.Where(f => f != "_headers");
    }

    private IEnumerable<string> FilesUnderWeb() =>
        Directory.EnumerateFiles(_repo.WebFolder, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(_repo.Root, p).Replace('\\', '/'));

    public static IEnumerable<object[]> Unpublished() =>
    [
        ["/README.md"],
        ["/src/Elliptic.Bsd/Curve.cs"],
        ["/tools/build-site.sh"],
        ["/.git/config"],
        ["/web/../README.md"],
        ["/web/nope.css"],
        ["/nope.html"],
    ];

    private async Task AssertServedIdentically(string repositoryRelativePath)
    {
        var response = await _client.GetAsync("/" + repositoryRelativePath);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"/{repositoryRelativePath} answered {(int)response.StatusCode}");
        Assert.Equal(
            await File.ReadAllBytesAsync(_repo.PathOf(repositoryRelativePath)),
            await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task EveryPublishedInputIsServedByteIdentical()
    {
        foreach (var input in PublishedInputs())
        {
            await AssertServedIdentically(input);
        }
    }

    [Fact]
    public async Task EveryFileUnderWebIsServedByteIdentical()
    {
        var files = FilesUnderWeb().ToList();
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            await AssertServedIdentically(file);
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    public async Task TheHomePageAnswersAtBothPaths(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            await File.ReadAllBytesAsync(_repo.PathOf("index.html")),
            await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [MemberData(nameof(Unpublished))]
    public async Task NothingElseInTheRepositoryIsServed(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            await File.ReadAllBytesAsync(_repo.PathOf("404.html")),
            await response.Content.ReadAsByteArrayAsync());
    }

    // P6-1. HttpClient normalises "/web/../README.md" to "/README.md" before it sends, so the
    // "/web/../README.md" row above only repeats the "/README.md" row. This sends the path to the
    // test server literally, the way a client that does not normalise (curl --path-as-is) would.
    [Fact]
    public async Task ATraversalPathSentLiterallyIs404WithTheBodyOf404Html()
    {
        var context = await _factory.Server.SendAsync(c =>
        {
            c.Request.Method = "GET";
            c.Request.Path = "/web/../README.md";
        });

        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        using var body = new MemoryStream();
        await context.Response.Body.CopyToAsync(body);
        Assert.Equal(await File.ReadAllBytesAsync(_repo.PathOf("404.html")), body.ToArray());
    }

    // P6-2. "Nothing else" is pinned both ways. EveryPublishedInputIsServedByteIdentical goes red
    // when the deploy publishes a file this host does not serve; this goes red when this host
    // serves a file the deploy does not publish.
    [Fact]
    public void EveryRootFileTheHostServesIsPublishedByTheDeploy()
        => Assert.Subset(PublishedInputs().ToHashSet(), RepositorySite.RootFiles.ToHashSet());

    [Fact]
    public async Task AnUnknownPageIs404WithTheBodyOf404Html()
    {
        var response = await _client.GetAsync("/nope");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            await File.ReadAllBytesAsync(_repo.PathOf("404.html")),
            await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public void LocatingFromOutsideAnyRepositoryRefusesAndNamesWhereItLooked()
    {
        var elsewhere = Path.GetPathRoot(Path.GetTempPath())!;
        var e = Assert.Throws<SiteNotServableException>(() => RepositorySite.Locate(elsewhere));
        Assert.Contains(Path.GetFullPath(elsewhere), e.Message);
        Assert.Contains(RepositorySite.RootMarker, e.Message);
    }

    // P4-4. A repository that lacks what the host serves is refused at start-up, naming every
    // missing item and the folder it looked in, not left to answer 500 on the first request.
    [Fact]
    public void ARepositoryMissingWhatItServesIsRefusedAndNamesEveryMissingItem()
    {
        var root = Directory.CreateTempSubdirectory("elliptic-site-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(root, RepositorySite.RootMarker), "");
            var e = Assert.Throws<SiteNotServableException>(() => RepositorySite.Locate(root));
            Assert.Contains(root, e.Message);
            Assert.Contains(RepositorySite.WebFolderName, e.Message);
            foreach (var file in RepositorySite.RootFiles)
            {
                Assert.Contains(file, e.Message);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
