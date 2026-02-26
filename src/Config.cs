namespace Whitelist;

public class PluginConfig
{
    public int Mode { get; set; } = 1;
    public string AddCommand { get; set; } = "wl";
    public string RemoveCommand { get; set; } = "uwl";
    public string PermissionForCommands { get; set; } = "admin.ban";
    // 增加這一行，讓程式可以讀取管理員清單
    public List<string> Admins { get; set; } = new(); 
}
