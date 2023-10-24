using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

public interface IASMPatch
{
    public abstract void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info);
}
