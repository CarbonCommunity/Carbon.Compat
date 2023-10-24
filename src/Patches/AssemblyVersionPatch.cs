using System.Reflection;
using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

public class AssemblyVersionPatch : IASMPatch
{
    public void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        List<Assembly> loaded = AppDomain.CurrentDomain.GetAssemblies().ToList();
        foreach (AssemblyReference asmRef in asm.AssemblyReferences)
        {
            foreach (Assembly lasm in loaded)
            {
                AssemblyName name = lasm.GetName();
                if (name.Name == asmRef.Name)
                {
                    //Logger.Info($"Setting version {asmRef.Name} to {asmRef.Version} > {name.Version}");
                    asmRef.Version = name.Version;
                }
            }
        }
    }
}
