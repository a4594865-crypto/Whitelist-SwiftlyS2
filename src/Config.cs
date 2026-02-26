using System;
using System.Collections.Generic;
using System.Text;

namespace Whitelist;

public class PluginConfig
{
    // 模式設定：1 為白名單，2 為黑名單
    public int Mode { get; set; } = 1;

    // 指令名稱
    public string AddCommand { get; set; } = "wladd";
    public string RemoveCommand { get; set; } = "wlremove";

    // 權限設定
    public string PermissionForCommands { get; set; } = "@css/root";
    
    // 新增這行，解決 'PluginConfig' 不包含 'AdminExemptPermission' 的錯誤
    public string AdminExemptPermission { get; set; } = "@css/root"; 
}
