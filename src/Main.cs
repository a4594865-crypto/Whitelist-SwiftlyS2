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

[PluginMetadata(Id = "Whitelist", Version = "1.3.2", Name = "Whitelist", Author = "verneri")]
public partial class Whitelist(ISwiftlyCore core) : BasePlugin(core) {

    private PluginConfig _config = null!;
    private HashSet<string> _whitelist = new();
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

        Core.Logger.LogInformation("[Whitelist] 載入完成。管理員豁免標籤: {Permission}", _config.AdminExemptPermission);
    }

    private void OnToggleWhitelist(ICommandContext context)
    {
        _isEnabled = !_isEnabled;
        string status = _isEnabled ? "{Lime}已開啟" : "{Red}已關閉";
        context.Reply($" {{LightBlue}}[白名單系統]{{Default}} 目前狀態：{status}");
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        if (!_isEnabled) return HookResult.Continue;
        if (@event == null) return HookResult.Continue;

        var player = @event.Accessor.GetPlayer("userid");
        if (player == null || !player.IsValid) return HookResult.Continue;

        // --- 核心修正：使用全域權限檢查 ---
        // 在 SwiftlyS2 1.1.3 中，這是最穩定的檢查方式
        // 傳入玩家的 SteamID 與 設定的權限標籤
        if (Core.Permission.PlayerHasPermission(player.SteamID, _config.AdminExemptPermission))
        {
            return HookResult.Continue; 
        }

        var steamId = player.SteamID.ToString();

        if (_config.Mode == 1 && !_whitelist.Contains(steamId))
        {
            player.Kick("{LightBlue}[白名單]{Default} 白名單模式已開啟，你不在准許名單中。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
        }
        else if (_config.Mode == 2 && _whitelist.Contains(steamId))
        {
            player.Kick("{LightBlue}[白名單]{Default} 你已被禁止進入此伺服器。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
        }

        return HookResult.Continue;
    }

    public override void Unload() { }
    private void OnMapLoad(IOnMapLoadEvent @event) { LoadWhitelist(); }

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
