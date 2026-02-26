namespace Whitelist;

public class PluginConfig
{
    public int Mode { get; set; } = 1;
    public string AddCommand { get; set; } = "wladd";
    public string RemoveCommand { get; set; } = "wlremove";
    public string PermissionForCommands { get; set; } = "@css/root";
    
    // 必須有這一行，否則 Main.cs 會找不到定義
    public string AdminExemptPermission { get; set; } = "@css/root"; 
}
