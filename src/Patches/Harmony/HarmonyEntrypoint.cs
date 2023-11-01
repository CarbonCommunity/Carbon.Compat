using API.Events;
using Carbon.Compat.Converters;
using Carbon.Compat.Lib;
using HarmonyLib;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;

namespace Carbon.Compat.Patches.Harmony;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public class HarmonyEntrypoint : BaseHarmonyPatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.Context context)
    {
        Guid guid = Guid.NewGuid();
        IEnumerable<TypeDefinition> entryPoints = asm.GetAllTypes().Where(x => x.Interfaces.Any(y=>y.Interface?.FullName == "Carbon.Compat.Lib.HarmonyCompat+IHarmonyModHooks"));

        CodeGenHelpers.GenerateEntrypoint(asm, importer, HarmonyStr, guid, out MethodDefinition load, out MethodDefinition unload, out TypeDefinition entryDef);

        load.CilMethodBody = new CilMethodBody(load);
        unload.CilMethodBody = new CilMethodBody(unload);
        unload.CilMethodBody.Instructions.Add(CilOpCodes.Ret);

        MethodDefinition postHookLoad = new MethodDefinition("postHookLoad", MethodAttributes.CompilerControlled, MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void, importer.ImportTypeSignature(typeof(EventArgs))));
        postHookLoad.CilMethodBody = new CilMethodBody(postHookLoad);

        FieldDefinition loadedField = new FieldDefinition("loaded", FieldAttributes.PrivateScope, new FieldSignature(asm.CorLibTypeFactory.Boolean));
        int postHookIndex = 0;

        CodeGenHelpers.GenerateCarbonEventCall(load.CilMethodBody, importer, ref postHookIndex, CarbonEvent.HookValidatorRefreshed, postHookLoad, new CilInstruction(CilOpCodes.Ldarg_0));

        load.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));

        CilInstruction postHookRet = new CilInstruction(CilOpCodes.Ret);
        postHookLoad.CilMethodBody.Instructions.AddRange(new[]
        {
            // load check
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldfld, loadedField),
            new CilInstruction(CilOpCodes.Brtrue_S, postHookRet.CreateLabel()),
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldc_I4_1),
            new CilInstruction(CilOpCodes.Stfld, loadedField),

            // harmony patch all
            new CilInstruction(CilOpCodes.Ldstr, $"__CCL:{asm.Assembly.Name}:{guid:N}"),
            new CilInstruction(CilOpCodes.Newobj, importer.ImportMethod(AccessTools.Constructor(typeof(HarmonyLib.Harmony), new Type[]{typeof(string)}))),
            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(AccessTools.Method(typeof(HarmonyLib.Harmony), "PatchAll")))
        });

        if (entryPoints.Any())
        {
            List<KeyValuePair<TypeDefinition, List<MethodDefinition>>> input = entryPoints.Select(entry => new KeyValuePair<TypeDefinition, List<MethodDefinition>>(entry, new List<MethodDefinition>
	            {
		            entry.Methods.First(x => x.Name == "OnLoaded")
	            }))
	            .ToList();

            int multiCallIndex = postHookLoad.CilMethodBody.Instructions.Count;
            CodeGenHelpers.DoMultiMethodCall(postHookLoad.CilMethodBody, ref multiCallIndex, null, input);
        }
        postHookLoad.CilMethodBody.Instructions.Add(postHookRet);
        entryDef.Methods.Add(postHookLoad);
        entryDef.Fields.Add(loadedField);
    }
}
