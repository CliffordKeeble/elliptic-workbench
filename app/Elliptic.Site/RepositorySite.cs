namespace Elliptic.Site;

/// <summary>
/// The repository folder this host serves from, and the list of what it serves.
/// Nothing else in the repository is reachable: the list is the whole surface.
/// </summary>
public sealed class RepositorySite
{
    /// <summary>The file whose folder is the repository root.</summary>
    public const string RootMarker = "elliptic-workbench.slnx";

    /// <summary>
    /// Files at fixed paths, served under their own repository-relative path
    /// ("index.html" at "/index.html"). The page that tools/build-site.sh publishes and this
    /// list must agree; a test reads that script and fails when they do not.
    /// </summary>
    public static readonly IReadOnlyList<string> RootFiles =
        ["index.html", "404.html", "data/bsd-rank0.json"];

    /// <summary>A folder served whole, with everything under it, at "/web".</summary>
    public const string WebFolderName = "web";

    /// <summary>The page behind "/" as well as "/index.html".</summary>
    public const string HomePage = "index.html";

    /// <summary>The page whose body answers any other path, with status 404.</summary>
    public const string NotFoundPage = "404.html";

    private RepositorySite(string root) => Root = root;

    public string Root { get; }

    public string WebFolder => Path.Combine(Root, WebFolderName);

    public string PathOf(string repositoryRelativePath) =>
        Path.Combine(Root, repositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> to the first folder holding
    /// <see cref="RootMarker"/>. Throws <see cref="RepositoryNotFoundException"/>, naming where it
    /// looked, if there is none.
    /// </summary>
    public static RepositorySite Locate(string startDirectory)
    {
        var start = Path.GetFullPath(startDirectory);
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, RootMarker)))
            {
                return new RepositorySite(dir.FullName);
            }
        }

        throw new RepositoryNotFoundException(
            $"Cannot find the repository: no folder from '{start}' up to the drive root holds {RootMarker}.");
    }
}

public sealed class RepositoryNotFoundException(string message) : Exception(message);
