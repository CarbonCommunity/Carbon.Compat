using System.Reflection;
using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

public class AssemblyVersionPatch : IAssemblyPatch
{
    public void Apply(ModuleDefinition assembly, ReferenceImporter importer, ref BaseConverter.Context context)
    {
        Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();

        foreach (AssemblyReference assemblyReference in assembly.AssemblyReferences)
        {
            foreach (Assembly lasm in loaded)
            {
                AssemblyName name = lasm.GetName();

                if (name.Name == assemblyReference.Name)
                {
                    assemblyReference.Version = name.Version;
                }
            }
        }
    }
}
