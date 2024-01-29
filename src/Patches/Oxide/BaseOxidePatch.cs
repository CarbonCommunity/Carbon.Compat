using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches.Oxide;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

public abstract class BaseOxidePatch : IAssemblyPatch
{
    public const string OxideStr = "Oxide";

    public abstract void Apply(ModuleDefinition assembly, ReferenceImporter importer, ref BaseConverter.Context context);
}
