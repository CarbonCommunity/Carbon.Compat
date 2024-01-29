using System.Collections.Immutable;
using AsmResolver.DotNet.Builder;
using AsmResolver.PE.DotNet.Builder;
using Carbon.Compat.Patches;

namespace Carbon.Compat.Converters;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

public abstract class BaseConverter
{
    public abstract ImmutableList<IAssemblyPatch> Patches { get;}

    public abstract string Name { get; }

    internal static ManagedPEImageBuilder _imageBuilder = new ManagedPEImageBuilder();
    internal static ManagedPEFileBuilder _fileBuilder = new ManagedPEFileBuilder();

    public byte[] Convert(ModuleDefinition asm, Context ctx = default)
    {
        ReferenceImporter importer = new ReferenceImporter(asm);

        foreach (IAssemblyPatch patch in Patches)
        {
            patch.Apply(asm, importer, ref ctx);
        }

        PEImageBuildResult result = _imageBuilder.CreateImage(asm);

        if (result.HasFailed)
        {
	        throw new MetadataBuilderException("it failed :(");
        }

        using (MemoryStream ms = new MemoryStream())
        {
            _fileBuilder.CreateFile(result.ConstructedImage).Write(ms);
            return ms.ToArray();
        }
    }

    public struct Context
    {
	    public string Author;
	    public bool noEntrypoint;
    }
}
