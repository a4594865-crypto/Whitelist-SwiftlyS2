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

[PluginMetadata(Id = "Whitelist", Version = "1.3.9", Name = "Whitelist", Author = "verneri & 秋風的夜 (Fix)")]
public partial class Whitelist(ISwiftlyCore core) : BasePlugin(core) {

    private PluginConfig _config = null!;
    private HashSet<string> _whitelist = new();
    
    // 記憶體變數，重啟伺服器預設為關閉
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

        Core.Logger.LogInformation("[Whitelist] 修正版載入完成。管理員豁免功能已優化。");
    }

    private void OnToggleWhitelist(ICommandContext context)
    {
        _isEnabled = !_isEnabled;
        string status = _isEnabled ? "{Lime}已開啟" : "{Red}已關閉";
        context.Reply($" {{LightBlue}}[白名單系統]{{Default}} 目前狀態：{status}");
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        // 1. 如果白名單沒開啟，直接放行
        if (!_isEnabled) return HookResult.Continue;
        
        if (@event == null) return HookResult.Continue;
        var player = @event.Accessor.GetPlayer("userid");
        if (player == null || !player.IsValid) return HookResult.Continue;

        // 2. 獲取 SteamID64 (ulong 格式)
        ulong steamId64 = player.SteamID;

        // 3. 管理員豁免檢查 (核心修正點)
        // 檢查是否擁有 config 設定的權限，或是否具備 Swiftly 的 admin 標籤
        if (Core.Permission.PlayerHasPermission(steamId64, _config.AdminExemptPermission) || 
            Core.Permission.PlayerHasPermission(steamId64, "admin.*"))
        {
            Core.Logger.LogInformation($"[Whitelist] 管理員 {player.PlayerName} ({steamId64}) 已自動豁免。");
            return HookResult.Continue; 
        }

        var sID = steamId64.ToString();

        // 4. 白名單模式判斷
        if (_config.Mode == 1 && !_whitelist.Contains(sID))
        {
            player.Kick("{LightBlue}[白名單]{Default} 白名單模式已開啟，你不在准許名單中。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
        }
        else if (_config.Mode == 2 && _whitelist.Contains(sID))
        {
            player.Kick("{LightBlue}[白名單]{Default} 你已被禁止進入此伺服器。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
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
            context.Reply($" {{LightBlue}}[白名單系統]{{Default}} 已新增 {{Green}}{context.Args[0]}"); 
        } 
    }

    private void OnUwlcommand(ICommandContext context) 
    { 
        if (context.Args.Length < 1) return; 
        if (_whitelist.Remove(context.Args[0])) 
        { 
            SaveWhitelist(); 
            context.Reply($" {{LightBlue}}[白名單系統]{{Default}} 已移除 {{Red}}{context.Args[0]}"); 
        } 
    }
}
