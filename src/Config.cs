using System;
using System.Collections.Generic;
using System.Text;

namespace Whitelist;

public class PluginConfig
{
    public int Mode { get; set; } = 1;
    public string AddCommand { get; set; } = "wladd";
    public string RemoveCommand { get; set; } = "wlremove";
    public string PermissionForCommands { get; set; } = "@css/root";
    
    // 確保這一行存在，主程式才能編譯
    public string AdminExemptPermission { get; set; } = "@css/root"; 
}
