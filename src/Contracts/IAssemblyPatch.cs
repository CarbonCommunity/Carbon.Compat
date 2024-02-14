using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

public interface IAssemblyPatch
{
    public abstract void Apply(ModuleDefinition assembly, ReferenceImporter importer, ref BaseConverter.Context context);
}
