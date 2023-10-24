using System.Collections.Immutable;
using AsmResolver.DotNet.Builder;
using AsmResolver.PE.DotNet.Builder;
using Carbon.Compat.Patches;

namespace Carbon.Compat.Converters;

public abstract class BaseConverter
{
    public abstract ImmutableList<IASMPatch> patches { get;}

    public abstract string Name { get; }

    public class GenInfo
    {
        //public AssemblyReference selfRef;

        public bool noEntryPoint = false;

        public string author = null;

        public TokenMapping mappings;

        public GenInfo()//;AssemblyReference self)
        {
            //selfRef = self;
        }
    }

    private static ManagedPEImageBuilder builder = new ManagedPEImageBuilder();
    private static ManagedPEFileBuilder file_builder = new ManagedPEFileBuilder();

    public byte[] Convert(ModuleDefinition asm)//, out GenInfo info)
    {
        ReferenceImporter importer = new ReferenceImporter(asm);
        GenInfo info = new GenInfo();

        foreach (IASMPatch patch in patches)
        {
            patch.Apply(asm, importer, info);
        }

        PEImageBuildResult result = builder.CreateImage(asm);

        if (result.HasFailed) throw new MetadataBuilderException("it failed :(");

        info.mappings = (TokenMapping)result.TokenMapping;

        using (MemoryStream ms = new MemoryStream())
        {
            file_builder.CreateFile(result.ConstructedImage).Write(ms);
            return ms.ToArray();
        }
    }
}
