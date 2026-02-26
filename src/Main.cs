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

[PluginMetadata(Id = "Whitelist", Version = "1.2.7", Name = "Whitelist", Author = "verneri")]
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

        // 註冊指令
        Core.Command.RegisterCommand($"{_config.AddCommand}", OnWlcommand, false, $"{_config.PermissionForCommands}");
        Core.Command.RegisterCommand($"{_config.RemoveCommand}", OnUwlcommand, false, $"{_config.PermissionForCommands}");
        Core.Command.RegisterCommand("whitelist", OnToggleWhitelist, false, $"{_config.PermissionForCommands}");

        Core.Logger.LogInformation("[Whitelist] 載入完成，管理員豁免權限設定為: {Permission}", _config.AdminExemptPermission);
    }

    private void OnToggleWhitelist(ICommandContext context)
    {
        _isEnabled = !_isEnabled;
        string status = _isEnabled ? "{Lime}已開啟" : "{Red}已關閉";
        context.Reply($" {{LightBlue}}[白名單系統]{{Default}} 目前狀態：{status}");
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        // 1. 如果插件關閉中，直接放行
        if (!_isEnabled) return HookResult.Continue;
        if (@event == null) return HookResult.Continue;

        var player = @event.Accessor.GetPlayer("userid");
        if (player == null || !player.IsValid) return HookResult.Continue;

        // 2. 核心修改：管理員優先豁免檢查
        // 只要擁有指定權限（如 @css/root），不論在不在名單內都允許進入
        if (player.HasPermission(_config.AdminExemptPermission))
        {
            return HookResult.Continue;
        }

        var steamId = player.SteamID.ToString();

        // 3. 根據模式判斷
        if (_config.Mode == 1) // 白名單模式
        {
            if (!_whitelist.Contains(steamId))
            {
                player.Kick("白名單已開啟，你不在准許名單中。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
            }
        }
        else if (_config.Mode == 2) // 黑名單模式
        {
            if (_whitelist.Contains(steamId))
            {
                player.Kick("你被禁止進入此伺服器。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
            }
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
            context.Reply($" {LightBlue}[白名單系統]{{Default}} 已新增 {{Green}}{context.Args[0]}"); 
        } 
    }
    
    private void OnUwlcommand(ICommandContext context) 
    { 
        if (context.Args.Length < 1) return; 
        if (_whitelist.Remove(context.Args[0])) 
        { 
            SaveWhitelist(); 
            context.Reply($" {LightBlue}[白名單系統]{{Default}} 已移除 {{Red}}{context.Args[0]}"); 
        } 
    }
}
