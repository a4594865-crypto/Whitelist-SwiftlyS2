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

[PluginMetadata(Id = "Whitelist", Version = "1.2.5", Name = "Whitelist", Author = "verneri")]
public partial class Whitelist(ISwiftlyCore core) : BasePlugin(core) {

    private PluginConfig _config = null!;
    private HashSet<string> _whitelist = new();
    
    // 關鍵設定：伺服器啟動時，記憶體變數預設為 false (關閉)
    // 只要不關閉伺服器進程，這個變數在換圖時會一直保留在記憶體中
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

        // 載入白名單 SteamID 名單
        LoadWhitelist();

        Core.Event.OnMapLoad += OnMapLoad;
        Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);

        // 註冊指令
        Core.Command.RegisterCommand($"{_config.AddCommand}", OnWlcommand, false, $"{_config.PermissionForCommands}");
        Core.Command.RegisterCommand($"{_config.RemoveCommand}", OnUwlcommand, false, $"{_config.PermissionForCommands}");
        
        // 註冊切換開關指令 (!whitelist)
        Core.Command.RegisterCommand("whitelist", OnToggleWhitelist, false, $"{_config.PermissionForCommands}");

        Core.Logger.LogInformation("[Whitelist] 插件已啟動。目前狀態：預設關閉 (伺服器重啟後會重設為此狀態)");
    }

    private void OnToggleWhitelist(ICommandContext context)
    {
        _isEnabled = !_isEnabled;
        string status = _isEnabled ? "{Lime}已開啟" : "{Red}已關閉";
        
        context.Reply($" {FetchColors.LightBlue}[白名單系統]{FetchColors.Default} 目前狀態：{status}");
        context.Reply($" {FetchColors.LightBlue}[狀態說明]{FetchColors.Default} 換圖會維持狀態，伺服器關閉重啟則會恢復預設關閉。");
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        // 只有在 _isEnabled 為 true 時才進行檢查
        if (!_isEnabled) return HookResult.Continue;

        if (@event == null) return HookResult.Continue;
        var player = @event.Accessor.GetPlayer("userid");
        if (player == null || !player.IsValid) return HookResult.Continue;

        var steamId = player.SteamID.ToString();

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
                player.Kick("你已被列入黑名單，禁止進入。", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
            }
        }

        return HookResult.Continue;
    }

    public override void Unload() { }

    private void OnMapLoad(IOnMapLoadEvent @event) 
    { 
        // 換圖時重新載入白名單檔案 (確保名單是最新的)
        LoadWhitelist(); 
        
        // 注意：這裡「不要」寫 _isEnabled = false;
        // 這樣換地圖時，_isEnabled 會維持原本管理員設定的狀態。
    }

    private void LoadWhitelist()
    {
        _whitelist.Clear();
        if (!File.Exists(WhitelistFilePath))
        {
            File.WriteAllText(WhitelistFilePath, "");
            return;
        }
        var lines = File.ReadAllLines(WhitelistFilePath);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed)) _whitelist.Add(trimmed);
        }
    }

    private void SaveWhitelist() { File.WriteAllLines(WhitelistFilePath, _whitelist); }

    private void OnWlcommand(ICommandContext context)
    {
        if (context.Args.Length < 1) return;
        var steamId = context.Args[0];
        if (_whitelist.Add(steamId)) { SaveWhitelist(); context.Reply($"已新增 {steamId} 到名單。"); }
    }

    private void OnUwlcommand(ICommandContext context)
    {
        if (context.Args.Length < 1) return;
        var steamId = context.Args[0];
        if (_whitelist.Remove(steamId)) { SaveWhitelist(); context.Reply($"已從名單移除 {steamId}。"); }
    }
}
