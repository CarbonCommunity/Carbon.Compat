using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches.Oxide;

public abstract class BaseOxidePatch : IASMPatch
{
    public const string OxideStr = "Oxide";
    public abstract void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info);
}
