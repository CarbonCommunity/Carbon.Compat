using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public class AssemblyVersionPatch : IAssemblyPatch
{
    public void Apply(ModuleDefinition assembly, ReferenceImporter importer, BaseConverter.Context context)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assemblyReference in assembly.AssemblyReferences)
        {
            foreach (var lasm in loaded)
            {
                var name = lasm.GetName();

                if (name.Name == assemblyReference.Name)
                {
                    assemblyReference.Version = name.Version;
                }
            }
        }
    }
}
