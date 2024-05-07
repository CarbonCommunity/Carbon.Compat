using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Carbon.Compat.Patches.Harmony;
using HarmonyLib;
using JetBrains.Annotations;

namespace Carbon.Compat.Lib;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class HarmonyCompat
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class HarmonyLoader
	{
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class HarmonyMod
		{
			public string Name { get; set; }

			public string HarmonyId { get; set; }

			public Harmony Harmony { get; set; }

			public Assembly Assembly { get; set; }

			public Type[] AllTypes { get; set; }

			public List<IHarmonyModHooks> Hooks { get; } = new();
		}

		public static List<HarmonyMod> loadedMods = new();
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public struct HarmonyModInfo
	{
		public string Name;
		public string Version;
	}

	public class OnHarmonyModLoadedArgs
	{
	}

	public class OnHarmonyModUnloadedArgs
	{
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public interface IHarmonyModHooks
	{
		public void OnLoaded(OnHarmonyModLoadedArgs args);
		public void OnUnloaded(OnHarmonyModUnloadedArgs args);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static DynamicMethod InstancePatchCompat(Harmony instance, MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null)
	{
		MethodBase calling = new StackTrace().GetFrame(1).GetMethod();
		HarmonyPatchProcessor.RegisterPatch(original, $"{calling.DeclaringType.Assembly.GetName().Name} - {calling}", instance);
		HookProcessor.HookReload();
		instance.Patch(original, prefix, postfix, transpiler);
		return null;
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

		MethodInfo[] methods = type.GetMethods();
		MethodInfo postfix = methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0);
		MethodInfo prefix = methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0);
		MethodInfo transpiler = methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyTranspiler), false).Length > 0);
		MethodInfo patchTargetMethod = methods.FirstOrDefault(x =>
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
				var assembly = type.Assembly.GetName().Name;
				HarmonyPatchProcessor.RegisterPatch(assembly, original.DeclaringType.Assembly.GetName().Name, original.Name,
					original.DeclaringType.FullName, $"{assembly} - {type.FullName}", instance);

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
