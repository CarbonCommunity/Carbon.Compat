using System.Collections.Immutable;
using Carbon.Compat.Patches;
using Carbon.Compat.Patches.Harmony;
using Carbon.Compat.Patches.Oxide;
using JetBrains.Annotations;

namespace Carbon.Compat.Converters;

[UsedImplicitly]
public class HarmonyConverter : BaseConverter
{
    public override ImmutableList<IASMPatch> patches => _patches;

    private readonly ImmutableList<IASMPatch> _patches = new List<IASMPatch>()
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

#if DEBUG
	    //debug
	    new ASMDebugPatch()
#endif
    }.ToImmutableList();

    public override string Name => "Harmony";
}
