using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Whitelist;

[PluginMetadata(Id = "Whitelist", Version = "1.4.0", Name = "Whitelist", Author = "verneri & 秋風的夜 (Fix)")]
public partial class Whitelist(ISwiftlyCore core) : BasePlugin(core) {

    private PluginConfig _config = null!;
    private HashSet<string> _whitelist = new();
    
    // 預設為 false。伺服器重啟時，記憶體重置，這裡會變回 false。
    private bool _isEnabled = false; 

    private string WhitelistFilePath => Path.Combine(Core.PluginPath, "whitelist.txt");

    public override void Load(bool hotReload)
    {
        const string ConfigFileName = "config.jsonc";
        const string ConfigSection = "Whitelist";
        
        Core.Configuration
            .InitializeJsonWithModel<PluginConfig>(ConfigFileName, ConfigSection)
            .Configure(cfg => cfg.AddJsonFile(
                Core.Configuration.GetConfigPath(ConfigFileName),
                optional: false,
                reloadOnChange: true));

        ServiceCollection services = new();
        services.AddSwiftly(Core)
            .AddOptionsWithValidateOnStart<PluginConfig>()
            .BindConfiguration(ConfigSection);
            
        var provider = services.BuildServiceProvider();
        _config = provider.GetRequiredService<IOptions<PluginConfig>>().Value;

        LoadWhitelist();

        Core.Event.OnMapLoad += OnMapLoad;
        Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);

        Core.Command.RegisterCommand($"{_config.AddCommand}", OnWlcommand, false, $"{_config.PermissionForCommands}");
        Core.Command.RegisterCommand($"{_config.RemoveCommand}", OnUwlcommand, false, $"{_config.PermissionForCommands}");
        Core.Command.RegisterCommand("whitelist", OnToggleWhitelist, false, $"{_config.PermissionForCommands}");

        Core.Logger.LogInformation("[Whitelist] 載入完成。已改用十六進位顏色代碼修正顯示問題。");
    }

    private void OnToggleWhitelist(ICommandContext context)
    {
        // 只有打指令才會切換狀態
        _isEnabled = !_isEnabled;

        // 【修正點】改用 CS2 原生顏色代碼
        // \x0B = 淺藍, \x06 = 亮綠, \x02 = 紅色, \x01 = 預設白
        string colorCode = _isEnabled ? "\x06" : "\x02";
        string statusText = _isEnabled ? "已開啟" : "已關閉";

        context.Reply($" \x01[\x061 v 1 對 戰 模 式 \x01] 白名單目前狀態：{colorCode}{statusText}");
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        // 檢查記憶體變數狀態
        if (!_isEnabled) return HookResult.Continue;
        
        if (@event == null) return HookResult.Continue;
        var player = @event.Accessor.GetPlayer("userid");
        if (player == null || !player.IsValid) return HookResult.Continue;

        // 管理員豁免檢查
        if (Core.Permission.PlayerHasPermission(player.SteamID, _config.AdminExemptPermission))
        {
            return HookResult.Continue; 
        }

        var steamId = player.SteamID.ToString();

        if (_config.Mode == 1 && !_whitelist.Contains(steamId))
        {
            // Kick 訊息也同步改用原生代碼
            player.Kick("\x0B[白名單]\x01 白名單模式已開啟，你不在准許名單中。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
        }
        else if (_config.Mode == 2 && _whitelist.Contains(steamId))
        {
            player.Kick("\x0B[白名單]\x01 你已被禁止進入此伺服器。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
        }

        return HookResult.Continue;
    }

    public override void Unload() { }

    private void OnMapLoad(IOnMapLoadEvent @event) 
    { 
        LoadWhitelist(); 
    }

    private void LoadWhitelist()
    {
        _whitelist.Clear();
        if (!File.Exists(WhitelistFilePath)) { File.WriteAllText(WhitelistFilePath, ""); return; }
        foreach (var line in File.ReadAllLines(WhitelistFilePath))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed)) _whitelist.Add(trimmed);
        }
    }

    private void SaveWhitelist() { File.WriteAllLines(WhitelistFilePath, _whitelist); }

    private void OnWlcommand(ICommandContext context) 
    { 
        if (context.Args.Length < 1) return; 
        if (_whitelist.Add(context.Args[0])) 
        { 
            SaveWhitelist(); 
            context.Reply($" \x0B[白名單系統]\x01 已新增 \x06{context.Args[0]}"); 
        } 
    }

    private void OnUwlcommand(ICommandContext context) 
    { 
        if (context.Args.Length < 1) return; 
        if (_whitelist.Remove(context.Args[0])) 
        { 
            SaveWhitelist(); 
            context.Reply($" \x0B[白名單系統]\x01 已移除 \x02{context.Args[0]}"); 
        } 
    }
}
