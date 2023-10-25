using System.Reflection;
using Carbon.Compat.Patches.Harmony;
using HarmonyLib;
using JetBrains.Annotations;

namespace Carbon.Compat.Lib;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class HarmonyCompat
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal static class HarmonyLoader
	{
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		internal class HarmonyMod
		{
			internal string Name { get; set; }

			internal string HarmonyId { get; set; }

			public Harmony Harmony { get; set; }

			internal Assembly Assembly { get; set; }

			internal Type[] AllTypes { get; set; }

			internal List<IHarmonyModHooks> Hooks { get; } = new();
		}

		internal static List<HarmonyMod> loadedMods = new();
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal struct HarmonyModInfo
	{
		internal string Name;
		internal string Version;
	}

	internal class OnHarmonyModLoadedArgs
	{
	}

	internal class OnHarmonyModUnloadedArgs
	{
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal interface IHarmonyModHooks
	{
		void OnLoaded(OnHarmonyModLoadedArgs args);
		void OnUnloaded(OnHarmonyModUnloadedArgs args);
	}

	internal static HashSet<Type> typeCache = new();

	public static void PatchProcessorCompat(Harmony instance, Type type, HarmonyMethod attributes)
	{
		if (typeCache.Contains(type))
		{
			return;
		}

		typeCache.Add(type);

		Logger.Debug("HarmonyCompat", $":START: Patching {type.FullName} using {instance.Id}\n\n");

		var methods = type.GetMethods();
		var postfix = methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0);
		var prefix = methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0);
		var transpiler = methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyTranspiler), false).Length > 0);
		var patchTargetMethod = methods.FirstOrDefault(x =>
			x.GetCustomAttributes(typeof(HarmonyTargetMethods), false).Length > 0 ||
			x.GetCustomAttributes(typeof(HarmonyTargetMethod), false).Length > 0);

		if (patchTargetMethod == null)
		{
			throw new NullReferenceException($"Failed to find target method in {type.FullName}");
		}

		IEnumerable<MethodBase> methodsToPatch = null;
		MethodBase single = null;

		if (patchTargetMethod.ReturnType == typeof(IEnumerable<MethodBase>))
		{
			methodsToPatch = ((IEnumerable<MethodBase>)patchTargetMethod.Invoke(null,
				patchTargetMethod.GetParameters().Length > 0 ? new object[1] : Array.Empty<object>()));
		}
		else if (patchTargetMethod.ReturnType == typeof(MethodBase))
		{
			single = (MethodBase)patchTargetMethod.Invoke(null,
				patchTargetMethod.GetParameters().Length > 0 ? new object[]
				{
					null
				} : Array.Empty<object>());
		}
		else
		{
			return;
		}

		void ProcessType(MethodBase original, bool pregen)
		{
			if (pregen)
			{
				HarmonyPatchProcessor.RegisterPatch(original.DeclaringType.Assembly.GetName().Name, original.Name,
					original.DeclaringType.FullName, $"{type.Assembly.GetName().Name} - {type.FullName}");

				return;
			}

			try
			{
				Logger.Debug("HarmonyCompat", $"Patching '{(original.DeclaringType == null ? "NULL" : original.DeclaringType.FullName)}' of '{original.Name}'");

				if (!original.IsDeclaredMember())
				{
					original = original.GetDeclaredMember();
				}

				PatchProcessor patcher = new PatchProcessor(instance, original);

				if (postfix != null)
				{
					Logger.Debug("HarmonyCompat","> postfix", 2);
					patcher.AddPostfix(postfix);
				}

				if (prefix != null)
				{
					Logger.Debug("HarmonyCompat","> prefix", 2);
					patcher.AddPrefix(prefix);
				}

				if (transpiler != null)
				{
					Logger.Debug("HarmonyCompat","> transpiler", 2);
					patcher.AddTranspiler(transpiler);
				}

				patcher.Patch();
			}
			catch (Exception ex)
			{
				Logger.Error($"[HarmonyCompat] Failed to patch '{original.Name}'", ex);
			}
		}

		bool pregen = true;

		loop:

		if (methodsToPatch != null)
		{
			if (methodsToPatch.Any() && !pregen)
			{
				Logger.Debug("HarmonyCompat", $"Bulk patching {methodsToPatch.Count():n0} methods");
			}
			foreach (MethodBase original in methodsToPatch)
			{
				ProcessType(original, pregen);
			}
		}
		else if (single != null)
		{
			ProcessType(single, pregen);
		}

		if (pregen)
		{
			pregen = false;
			HookProcessor.HookReload();
			goto loop;
		}

		Logger.Debug("HarmonyCompat", $"Patch '{type.FullName}' complete with domain '{instance.Id}'");
	}
}
