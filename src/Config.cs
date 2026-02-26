using System;

namespace Whitelist;

public class PluginConfig
{
    public int Mode { get; set; } = 1;
    public string AddCommand { get; set; } = "wladd";
    public string RemoveCommand { get; set; } = "wlremove";
    public string PermissionForCommands { get; set; } = "admin";
    
    // 預設為 root 權限的人可以無視白名單
    public string AdminExemptPermission { get; set; } = "root"; 
}
