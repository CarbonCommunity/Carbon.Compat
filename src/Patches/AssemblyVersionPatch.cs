using System.Reflection;
using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

public class AssemblyVersionPatch : IAssemblyPatch
{
    public void Apply(ModuleDefinition assembly, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asmRef in assembly.AssemblyReferences)
        {
            foreach (var lasm in loaded)
            {
                var name = lasm.GetName();

                if (name.Name == asmRef.Name)
                {
                    asmRef.Version = name.Version;
                }
            }
        }
    }
}
