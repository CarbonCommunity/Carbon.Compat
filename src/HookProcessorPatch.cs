using API.Events;
using API.Hooks;
using Carbon.Compat.Patches.Harmony;

namespace Carbon.Compat;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

internal static class HookProcessor
{
    public static void HookReload()
    {
        Logger.Debug("Processing dynamic hooks", 2);

        foreach (var hook in Community.Runtime.HookManager.LoadedDynamicHooks)
        {
#if DEBUG
	        Logger.Debug($"Found dyn hooky: {hook.HookFullName}", 2);
#endif
	        if (hook.TargetMethods.Count == 0)
	        {
		        return;
	        }

            var cache = hook.TargetMethods[0];
            var asmName = cache.DeclaringType.Assembly.GetName().Name;
            var typeName = cache.DeclaringType.FullName;
            var methodName = cache.Name;

            var patchInfo = HarmonyPatchProcessor.CurrentPatches.FirstOrDefault(x => x.AssemblyName == asmName && x.TypeName == typeName && x.MethodName == methodName);

            if (patchInfo == null)
            {
	            continue;
            }

#if DEBUG
            Logger.Debug($"{patchInfo.Reason} Forcing hook {hook.TargetMethods[0]} to static", 2);
#endif

            if ((hook.Options & HookFlags.Patch) != HookFlags.Patch)
            {
	            Community.Runtime.HookManager.Subscribe(hook.Identifier, "CCL.Static");
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

        Logger.Debug("Patched HookExCTOR", 2);
    }
}
