using Commis.Desktop;

namespace Commis.Desktop.Tests;

public sealed class CommisLocationTests : IDisposable
{
    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("commis-location-");

    public void Dispose() => root.Delete(recursive: true);

    [Fact]
    public void TheVariableWinsOverEverything()
    {
        var app = Touch("app", CommisLocation.FileName);

        var found = CommisLocation.Find("/somewhere/commis", Path.GetDirectoryName(app)!, null);

        Assert.Equal("/somewhere/commis", found);
    }

    [Fact]
    public void ABlankVariableIsIgnored()
    {
        var beside = Touch("app", CommisLocation.FileName);

        Assert.Equal(beside, CommisLocation.Find("  ", Path.GetDirectoryName(beside)!, null));
    }

    [Fact]
    public void CommisBesideTheWindowComesBeforeABuild()
    {
        var beside = Touch("app/bin", CommisLocation.FileName);
        Touch(".build/debug", CommisLocation.FileName);

        Assert.Equal(beside, CommisLocation.Find(null, Path.GetDirectoryName(beside)!, null));
    }

    [Fact]
    public void ASwiftBuildAboveTheWindowIsFound()
    {
        var debug = Touch(".build/debug", CommisLocation.FileName);
        var app = Directory.CreateDirectory(Path.Combine(root.FullName, "app/Commis.Desktop/bin/Debug/net10.0"));

        Assert.Equal(debug, CommisLocation.Find(null, app.FullName, null));
    }

    [Fact]
    public void TheNewerOfTwoBuildsIsChosen()
    {
        var debug = Touch(".build/debug", CommisLocation.FileName);
        var release = Touch(".build/release", CommisLocation.FileName);
        File.SetLastWriteTimeUtc(release, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(debug, DateTime.UtcNow);

        Assert.Equal(debug, CommisLocation.Find(null, root.FullName, null));
    }

    [Fact]
    public void ThePathIsSearchedLast()
    {
        var app = Directory.CreateDirectory(Path.Combine(root.FullName, "app"));
        var onPath = Touch("bin", CommisLocation.FileName);
        var searchPath = string.Join(Path.PathSeparator, Path.Combine(root.FullName, "empty"), Path.GetDirectoryName(onPath));

        Assert.Equal(onPath, CommisLocation.Find(null, app.FullName, searchPath));
    }

    [Fact]
    public void NowhereIsNull()
    {
        var app = Directory.CreateDirectory(Path.Combine(root.FullName, "app"));

        Assert.Null(CommisLocation.Find(null, app.FullName, Path.Combine(root.FullName, "empty")));
    }

    private string Touch(string folder, string name)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root.FullName, folder));
        var path = Path.Combine(directory.FullName, name);
        File.WriteAllText(path, string.Empty);
        return path;
    }
}
