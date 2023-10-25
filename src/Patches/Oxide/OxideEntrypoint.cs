using API.Assembly;
using API.Events;
using Carbon.Compat.Converters;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;

namespace Carbon.Compat.Patches.Oxide;

public class OxideEntrypoint : BaseOxidePatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        var guid = Guid.NewGuid();
        var entryPoints = asm.GetAllTypes().Where(x=>x.BaseType?.FullName == "Oxide.Core.Extensions.Extension" && x.BaseType.DefinitionAssembly().Name == "Carbon.Common");

        if (!entryPoints.Any())
        {
            return;
        }

        info.author ??= entryPoints.FirstOrDefault().Properties.FirstOrDefault(x => x.Name == "Author" && x.GetMethod is { IsVirtual: true })?.GetMethod?.CilMethodBody?.Instructions.FirstOrDefault(x => x.OpCode == CilOpCodes.Ldstr)?.Operand as string;

        CodeGenHelpers.GenerateEntrypoint(asm, importer, OxideStr, guid, out MethodDefinition load, out MethodDefinition unload, out TypeDefinition entryPoint);

        entryPoint.Interfaces.Add(new InterfaceImplementation(importer.ImportType(typeof(ICarbonExtension))));

        load.CilMethodBody = new CilMethodBody(load);
        unload.CilMethodBody = new CilMethodBody(unload);
        unload.CilMethodBody.Instructions.Add(CilOpCodes.Ret);

        var serverInit = new MethodDefinition("serverInit",
            MethodAttributes.CompilerControlled, MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void, importer.ImportTypeSignature(typeof(EventArgs))));
        serverInit.CilMethodBody = new CilMethodBody(serverInit);

        var loadedField = new FieldDefinition("loaded", FieldAttributes.PrivateScope, new FieldSignature(asm.CorLibTypeFactory.Boolean));
        var postHookIndex = 0;

        CodeGenHelpers.GenerateCarbonEventCall(load.CilMethodBody, importer, ref postHookIndex, CarbonEvent.HookValidatorRefreshed, serverInit, new CilInstruction(CilOpCodes.Ldarg_0));

        load.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));

        var postHookRet = new CilInstruction(CilOpCodes.Ret);
        serverInit.CilMethodBody.Instructions.AddRange(new[]
        {
            // load check
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldfld, loadedField),
            new CilInstruction(CilOpCodes.Brtrue_S, postHookRet.CreateLabel()),
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldc_I4_1),
            new CilInstruction(CilOpCodes.Stfld, loadedField)
        });

        foreach (var entry in entryPoints)
        {
            var extLoadMethod = entry.Methods.FirstOrDefault(x => x.Name == "Load" && x.IsVirtual);
            var extCtor = entry.Methods.FirstOrDefault(x => x.Name == ".ctor" && x.Parameters.Count == 1);

            if (extLoadMethod == null)
            {
	            continue;
            }

            serverInit.CilMethodBody.Instructions.AddRange(new[]
            {
                new CilInstruction(CilOpCodes.Ldnull),
                new CilInstruction(CilOpCodes.Newobj, extCtor),
                new CilInstruction(CilOpCodes.Callvirt, extLoadMethod)
            });
        }

        serverInit.CilMethodBody.Instructions.Add(postHookRet);
        entryPoint.Fields.Add(loadedField);
        entryPoint.Methods.Add(serverInit);
    }
}
