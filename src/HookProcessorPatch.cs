using System.Reflection;
using API.Events;
using API.Hooks;
using Carbon.Compat.Lib;
using Carbon.Compat.Patches.Harmony;

namespace Carbon.Compat;

internal static class HookProcessor
{
    public static bool InitialHooksInstalled;

    public static void HookReload()
    {
    #if DEBUG
        Logger.Debug("Processing dynamic hooks", 2);
    #endif
        foreach (IHook Hooky in Community.Runtime.HookManager.LoadedDynamicHooks)
        {
        #if DEBUG
	        Logger.Debug($"Found dyn hooky: {Hooky.HookFullName}", 2);
        #endif
            if (Hooky.TargetMethods.Count == 0) return;
            MethodBase cache = Hooky.TargetMethods[0];
            string asmName = cache.DeclaringType.Assembly.GetName().Name;
            string typeName = cache.DeclaringType.FullName;
            string methodName = cache.Name;
            HarmonyPatchProcessor.PatchInfoEntry patchInfo = HarmonyPatchProcessor.CurrentPatches.FirstOrDefault(x =>
                x.ASMName == asmName && x.TypeName == typeName && x.MethodName == methodName);
            if (patchInfo != null)
            {
            #if DEBUG
	            Logger.Debug($"{patchInfo.reason} Forcing hook {Hooky.TargetMethods[0]} to static", 2);
            #endif
                if ((Hooky.Options & HookFlags.Patch) != HookFlags.Patch)
                    Community.Runtime.HookManager.Subscribe(Hooky.Identifier, "CCL.Static");
            }
        }

        Community.Runtime.HookManager.ForceUpdateHooks();
    }

    internal static void ApplyPatch()
    {
        Community.Runtime.Events.Subscribe(CarbonEvent.HookValidatorRefreshed, args =>
        {
            HookReload();
        });
    #if DEBUG
        Logger.Warn("Patched HookExCTOR");
    #endif
    }
}
