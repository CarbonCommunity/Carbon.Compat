using System.Collections.Immutable;
using Carbon.Compat.Patches;
using Carbon.Compat.Patches.Harmony;
using Carbon.Compat.Patches.Oxide;
using JetBrains.Annotations;

namespace Carbon.Compat.Converters;
[UsedImplicitly]
public class OxideConverter : BaseConverter
{
	public override ImmutableList<IASMPatch> patches => _patches;

	private readonly ImmutableList<IASMPatch> _patches = new List<IASMPatch>()
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
        new OxidePluginAttr(),

        //common
        new ReflectionFlagsPatch(),
        new AssemblyVersionPatch(),

        //debug
    #if DEBUG
        new ASMDebugPatch()
    #endif
    }.ToImmutableList();
    public override string Name => "oxide";
    //public override bool PluginReference => true;
}
