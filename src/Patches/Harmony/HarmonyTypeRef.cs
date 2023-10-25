using Carbon.Compat.Converters;
using Carbon.Compat.Lib;

namespace Carbon.Compat.Patches.Harmony;

public class HarmonyTypeRef : BaseHarmonyPatch
{
    //public TypeReference harmonyCompatRef = MainConverter.SelfModule.TopLevelTypes.First(x =>
    //    x.Namespace == "Carbon.Compat.Lib" && x.Name == nameof(HarmonyCompat)).ToTypeReference();
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeReference tw in asm.GetImportedTypeReferences())
        {
            AssemblyReference aref = tw.Scope as AssemblyReference;
            if (aref != null && aref.Name == HarmonyASM)
            {
                if (tw.Namespace.StartsWith(Harmony1NS)) tw.Namespace = Harmony2NS; // Namespace override
                if (tw.Name == "HarmonyInstance")
                {
                    tw.Name = "Harmony";
                }
            }
            if (aref != null && aref.Name == "Rust.Harmony")
            {
                tw.Namespace = $"Carbon.Compat.Lib";
                tw.Scope = (IResolutionScope)importer.ImportType(typeof(HarmonyCompat));
            }
        }
    }
}
