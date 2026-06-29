using ISO11820.Config;
using ISO11820.Core;
using ISO11820.Data;
using Serilog;

namespace ISO11820;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("Logs\\iso11820-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("ISO 11820 系统启动");

            // 加载配置
            AppConfig.Initialize();
            Log.Information("配置加载完成");

            // 初始化数据库
            string dbPath = DbPathHelper.GetDbPath(AppConfig.SqlitePath);
            AppGlobal.Instance.Initialize(dbPath);
            Log.Information("数据库初始化完成，路径：{DbPath}", dbPath);

            // 确保文件存储目录存在
            EnsureDirectoriesExist();

            // 启动 WinForms
            ApplicationConfiguration.Initialize();
            Application.Run(new Forms.LoginForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "系统启动失败");
            MessageBox.Show($"系统启动失败：\n{ex.Message}", "致命错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.Information("ISO 11820 系统关闭");
            Log.CloseAndFlush();
        }
    }

    private static void EnsureDirectoriesExist()
    {
        try
        {
            string baseDir = AppConfig.BaseDirectory;
            string testDataDir = AppConfig.TestDataDirectory;
            string outputDir = AppConfig.OutputDirectory;

            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
            if (!Directory.Exists(testDataDir))
                Directory.CreateDirectory(testDataDir);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            Log.Information("文件存储目录已确保存在");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "创建文件存储目录时出现警告");
        }
    }
}