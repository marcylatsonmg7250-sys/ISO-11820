namespace ISO11820.Data;

public static class DbPathHelper
{
    /// <summary>
    /// 获取数据库文件的完整路径，自动创建目录
    /// </summary>
    public static string GetDbPath(string relativePath)
    {
        // 相对路径转绝对路径
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

        // 确保目录存在
        string? dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return fullPath;
    }
}