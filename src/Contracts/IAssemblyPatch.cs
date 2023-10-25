using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public interface IAssemblyPatch
{
    public abstract void Apply(ModuleDefinition assembly, ReferenceImporter importer, BaseConverter.Context context);
}
