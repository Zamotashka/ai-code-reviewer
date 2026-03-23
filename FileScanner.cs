using System.IO;
public class FileScanner
{
    public List<FileInfo> Scan(DirectoryInfo directory, string extension)
    {
        var ext = extension.TrimStart('.');
        return directory
            .GetFiles($"*.{ext}", SearchOption.AllDirectories)
            .Where(f => !f.FullName.Contains("obj") &&
                        !f.FullName.Contains("bin") &&
                        !f.FullName.Contains(".git"))
            .ToList();
    }
}