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
public class OxideConverter : BaseConverter
{
	public override ImmutableList<IAssemblyPatch> Patches => _patches;

	public override string Name => "Oxide";

	private readonly ImmutableList<IAssemblyPatch> _patches = new List<IAssemblyPatch>()
	{
        // type ref
        new OxideTypeRef(),
        new HarmonyTypeRef(),

        // member ref

        //new OxideMemberRef(),

        // il switch
        new OxideILSwitch(),
        new HarmonyILSwitch(),

        // harmony
        new HarmonyPatchProcessor(),

        // entrypoint
        new OxideEntrypoint(),

        // plugins
        new OxidePluginAttribute(),

        //common
        new ReflectionFlagsPatch(),
        new AssemblyVersionPatch(),

		//debug
        new AssemblyDebugPatch()
	}.ToImmutableList();
}
