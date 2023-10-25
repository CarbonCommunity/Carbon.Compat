using System.Collections.Immutable;
using AsmResolver.DotNet.Builder;
using AsmResolver.PE.DotNet.Builder;
using Carbon.Compat.Patches;

namespace Carbon.Compat.Converters;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public abstract class BaseConverter
{
    public abstract ImmutableList<IAssemblyPatch> Patches { get;}

    public abstract string Name { get; }

    internal static ManagedPEImageBuilder _imageBuilder = new ManagedPEImageBuilder();
    internal static ManagedPEFileBuilder _fileBuilder = new ManagedPEFileBuilder();

    public byte[] Convert(ModuleDefinition asm)
    {
        var importer = new ReferenceImporter(asm);
        var info = (Context)default;

        foreach (IAssemblyPatch patch in Patches)
        {
            patch.Apply(asm, importer, info);
        }

        var result = _imageBuilder.CreateImage(asm);

        if (result.HasFailed)
        {
	        throw new MetadataBuilderException("it failed :(");
        }

        using (var ms = new MemoryStream())
        {
            _fileBuilder.CreateFile(result.ConstructedImage).Write(ms);
            return ms.ToArray();
        }
    }

    public struct Context
    {
	    public string Author;
    }
}
