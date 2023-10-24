using System.Reflection;
using System.Runtime.CompilerServices;
using Carbon.Core;
using JetBrains.Annotations;
using Oxide.Core.Extensions;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using Timer = Oxide.Plugins.Timer;

namespace Carbon.Compat.Lib;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static partial class OxideCompat
{
	internal const string legacy_msg = "Used for oxide backwards compatibility";
    internal static Dictionary<Assembly, ModLoader.ModPackage> modPackages = new();
    public static void RegisterPluginLoader(ExtensionManager self, PluginLoader loader, Extension oxideExt)
    {
        self.RegisterPluginLoader(loader);
        string asmName = Assembly.GetCallingAssembly().GetName().Name;
        Logger.Debug($"Oxide plugin loader call using {loader.GetType().FullName} from assembly {asmName}", 2);
        Assembly asm = oxideExt != null ? oxideExt.GetType().Assembly : loader.GetType().Assembly;
        string name = oxideExt != null ? oxideExt.Name : asm.GetName().Name;
        string author = oxideExt != null ? oxideExt.Author : "CCL";
        if (!modPackages.TryGetValue(asm, out ModLoader.ModPackage package))
        {
            package = new ModLoader.ModPackage
            {
                Name = $"{name} - {author} (CCL Oxide Extension)"
            };
            ModLoader.LoadedPackages.Add(package);
            modPackages[asm] = package;
        }
        foreach (Type type in loader.CorePlugins)
        {
            if (type.IsAbstract) continue;
            try
            {
                ModLoader.InitializePlugin(type, out RustPlugin plugin, package, precompiled:true, preInit: oxideExt == null ? null :
                    rustPlugin =>
                    {
                        rustPlugin.Version = oxideExt.Version;
                        if (rustPlugin.Author == "CCL" && !string.IsNullOrWhiteSpace(oxideExt.Author))
                            rustPlugin.Author = oxideExt.Author;
                    });
                plugin.IsExtension = true;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load plugin {type.Name} in oxide extension {asmName}: {e}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddConsoleCommand1(global::Oxide.Game.Rust.Libraries.Command Lib, string name, Plugin plugin, Func<ConsoleSystem.Arg, bool> callback)
    {
        Lib.AddConsoleCommand(name, plugin, callback);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddChatCommand1(global::Oxide.Game.Rust.Libraries.Command Lib, string name, Plugin plugin, Action<BasePlayer, string, string[]> callback)
    {
        Lib.AddChatCommand(name, plugin, callback);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T OxideCallHookGeneric<T>(string hook, params object[] args)
    {
        return (T)global::Oxide.Core.Interface.Call<T>(hook, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetExtensionDirectory(global::Oxide.Core.OxideMod _)
    {
        return Defines.GetExtensionsFolder();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer TimerOnce(Timers instance, float delay, Action callback, Plugin owner = null)
    {
        return instance.Once(delay, callback);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer TimerRepeat(Timers instance, float delay, int reps, Action callback, Plugin owner = null)
    {
        return instance.Repeat(delay, reps, callback);
    }
}
