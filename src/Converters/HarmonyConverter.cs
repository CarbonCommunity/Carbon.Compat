using System.Collections.Immutable;
using Carbon.Compat.Patches;
using Carbon.Compat.Patches.Harmony;
using Carbon.Compat.Patches.Oxide;
using JetBrains.Annotations;

namespace Carbon.Compat.Converters;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
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
}
