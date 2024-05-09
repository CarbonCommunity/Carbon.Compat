using System.Collections.Immutable;
using Carbon.Compat.Patches;
using Carbon.Compat.Patches.Harmony;
using Carbon.Compat.Patches.Oxide;
using JetBrains.Annotations;

namespace Carbon.Compat.Converters;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

[UsedImplicitly]
public class HarmonyConverter : BaseConverter
{
    public override ImmutableList<IAssemblyPatch> Patches => _patches;

    private readonly ImmutableList<IAssemblyPatch> _patches = new List<IAssemblyPatch>()
    {
	    // type ref
	    new HarmonyTypeRef(),
	    new OxideTypeRef(),

	    // il switch
	    new HarmonyILSwitch(),
	    new OxideILSwitch(),

	    // harmony
	    new HarmonyPatchProcessor(),

	    // entrypoint
	    new HarmonyEntrypoint(),

	    //common
	    new ReflectionFlagsPatch(),
	    new AssemblyVersionPatch(),

	    //debug
	    new AssemblyDebugPatch()
    }.ToImmutableList();

    public override string Name => "HarmonyMod";

    public static readonly Version V2 = new(2, 0, 0);

    public static bool IsV2Harmony(ModuleDefinition asm)
    {
	    return asm.AssemblyReferences.Any(x => x.Name == "0Harmony" && x.Version > V2);
    }
}
