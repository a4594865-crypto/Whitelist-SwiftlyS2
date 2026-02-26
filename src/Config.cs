using System;
using System.Collections.Generic;
using System.Text;

namespace Whitelist;

public class PluginConfig
{
    // 儲存白名單開關狀態，預設為 true (換地圖自動開啟)
    public bool Enabled { get; set; } = true; 

    public int Mode { get; set; } = 1;
    public string AddCommand { get; set; } = "wl";
    public string RemoveCommand { get; set; } = "uwl";
    public string PermissionForCommands { get; set; } = "admin.ban";
    
    // 管理員豁免權限標籤
    public string AdminExemptPermission { get; set; } = "root"; 
}
