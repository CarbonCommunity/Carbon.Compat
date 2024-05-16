using System.Reflection;
using API.Events;
using API.Hooks;
using Carbon.Compat.Patches.Harmony;

namespace Carbon.Compat;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

internal static class HookProcessor
{
	public static void HookClear()
	{
		Logger.Debug("Unprocessing dynamic hooks", 2);

		foreach (IHook hook in Community.Runtime.HookManager.LoadedDynamicHooks)
		{
			if (hook.TargetMethods.Count == 0)
			{
				return;
			}

			MethodBase cache = hook.TargetMethods[0];
			string asmName = cache.DeclaringType.Assembly.GetName().Name;
			string typeName = cache.DeclaringType.FullName;
			string methodName = cache.Name;

			Components.Harmony.PatchInfoEntry patchInfo = Components.Harmony.CurrentPatches.FirstOrDefault(x => x.AssemblyName == asmName && x.TypeName == typeName && x.MethodName == methodName);

			if (patchInfo == null)
			{
				continue;
			}

			if ((hook.Options & HookFlags.Patch) != HookFlags.Patch)
			{
				Community.Runtime.HookManager.Unsubscribe(hook.Identifier, "CCL.Static");
			}
		}
	}

    public static void HookReload()
    {
        Logger.Debug("Processing dynamic hooks", 2);

        foreach (IHook hook in Community.Runtime.HookManager.LoadedDynamicHooks)
        {
#if DEBUG
	        Logger.Debug($"Found dyn hooky: {hook.HookFullName}", 2);
#endif

	        if (hook == null || hook.TargetMethods == null || hook.TargetMethods.Count == 0)
	        {
		        return;
	        }

            MethodBase cache = hook.TargetMethods[0];
            string asmName = cache.DeclaringType.Assembly.GetName().Name;
            string typeName = cache.DeclaringType.FullName;
            string methodName = cache.Name;

            Components.Harmony.PatchInfoEntry patchInfo = Components.Harmony.CurrentPatches.FirstOrDefault(x => x.AssemblyName == asmName && x.TypeName == typeName && x.MethodName == methodName);

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
}
