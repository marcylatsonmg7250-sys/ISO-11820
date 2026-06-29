using ISO11820.Data;

namespace ISO11820.Core;

/// <summary>
/// 全局应用上下文（单例）
/// </summary>
public class AppGlobal
{
    private static AppGlobal? _instance;
    private static readonly object _lock = new();

    public static AppGlobal Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new AppGlobal();
                }
            }
            return _instance;
        }
    }

    public DbHelper Db { get; private set; } = null!;
    public string CurrentUserId { get; set; } = "";
    public string CurrentUsername { get; set; } = "";
    public string CurrentUserType { get; set; } = "";
    public Models.Apparatus? CurrentApparatus { get; set; }

    private AppGlobal() { }

    public void Initialize(string dbPath)
    {
        Db = new DbHelper(dbPath);
        Db.InitializeDatabase();
        CurrentApparatus = Db.GetApparatus();
    }
}