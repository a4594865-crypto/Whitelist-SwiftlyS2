using System;
using System.Collections.Generic;
using System.Text;

namespace Whitelist;

public class PluginConfig
{
    // 模式設定：1 = 白名單, 2 = 黑名單
    public int Mode { get; set; } = 1;

    // 新增名單的指令 (原本是 "wl")
    public string AddCommand { get; set; } = "wl";

    // 移除名單的指令 (原本是 "uwl")
    public string RemoveCommand { get; set; } = "uwl";

    // 執行指令管理名單所需的權限 (原本是 "admin.ban")
    public string PermissionForCommands { get; set; } = "admin.ban";
    
    // --- 新增的部分 ---
    // 管理員豁免權限：具備此權限的人不會被踢出伺服器
    // 預設設為 "root"，這在 SwiftlyS2 中通常代表最高管理員
    public string AdminExemptPermission { get; set; } = "root"; 
}
