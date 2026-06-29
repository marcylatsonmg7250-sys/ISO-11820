using Microsoft.Extensions.Configuration;

namespace ISO11820.Config;

/// <summary>
/// 应用程序配置（从 appsettings.json 读取）
/// </summary>
public static class AppConfig
{
    private static IConfiguration? _config;

    public static void Initialize()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        _config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    public static void Reload()
    {
        Initialize();
    }

    // ==================== Database ====================
    public static string DatabaseProvider => _config?["Database:Provider"] ?? "Sqlite";
    public static string SqlitePath => _config?["Database:SqlitePath"] ?? "Data\\ISO11820.db";

    // ==================== Hardware ====================
    public static int ConstPower => int.TryParse(_config?["Hardware:ConstPower"], out var v) ? v : 2048;
    public static int PidTemperature => int.TryParse(_config?["Hardware:PidTemperature"], out var v) ? v : 750;

    // ==================== Simulation ====================
    public static bool EnableSimulation => bool.TryParse(_config?["Simulation:EnableSimulation"], out var v) && v;
    public static bool SimulateSensors => bool.TryParse(_config?["Simulation:SimulateSensors"], out var v) && v;
    public static bool SimulatePidController => bool.TryParse(_config?["Simulation:SimulatePidController"], out var v) && v;
    public static double InitialFurnaceTemp => double.TryParse(_config?["Simulation:InitialFurnaceTemp"], out var v) ? v : 720.0;
    public static double TargetFurnaceTemp => double.TryParse(_config?["Simulation:TargetFurnaceTemp"], out var v) ? v : 750.0;
    public static double HeatingRatePerSecond => double.TryParse(_config?["Simulation:HeatingRatePerSecond"], out var v) ? v : 40.0;
    public static double TempFluctuation => double.TryParse(_config?["Simulation:TempFluctuation"], out var v) ? v : 0.5;
    public static double StableThreshold => double.TryParse(_config?["Simulation:StableThreshold"], out var v) ? v : 3.0;

    // ==================== FileStorage ====================
    public static string BaseDirectory => ResolvePath(_config?["FileStorage:BaseDirectory"] ?? ".\\Data");
    public static string TestDataDirectory => ResolvePath(_config?["FileStorage:TestDataDirectory"] ?? ".\\Data\\TestData");

    // ==================== Report ====================
    public static string OutputDirectory => ResolvePath(_config?["Report:OutputDirectory"] ?? ".\\Data\\Reports");
    public static bool EnablePdfExport => !bool.TryParse(_config?["Report:EnablePdfExport"], out var v) || v;

    // ==================== 派生配置 ====================
    public static double StableTempMin => TargetFurnaceTemp - StableThreshold;  // 745
    public static double StableTempMax => TargetFurnaceTemp + StableThreshold;  // 755

    /// <summary>将相对路径解析为绝对路径（相对于应用程序目录）</summary>
    private static string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return AppDomain.CurrentDomain.BaseDirectory;
        if (Path.IsPathRooted(path)) return path;
        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
    }
}