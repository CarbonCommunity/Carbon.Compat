using System.Diagnostics;
using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;
public class ASMDebugPatch : IASMPatch
{
    public void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            foreach (MethodDefinition method in td.Methods)
            {
                CilMethodBody body = method.CilMethodBody;
                if (body == null) continue;
                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    CilInstruction CIL = body.Instructions[index];
                    if (CIL.OpCode == CilOpCodes.Call && CIL.Operand is MemberReference mref && mref.DeclaringType.DefinitionAssembly().IsCorLib &&
                        mref.Signature is MethodSignature msig && (( mref.DeclaringType.Name == "Debugger" && ( mref.Name == "get_IsAttached" || mref.Name == "IsLogging" )) ||
                                                                   ( mref.DeclaringType.Name == "Environment" && mref.Name == "FailFast" ) ) )
                    {
                        //Logger.Debug($"Found {CIL}");
                        for (int pc = 0; pc < msig.ParameterTypes.Count; pc++)
                        {
                            body.Instructions.Insert(index, new CilInstruction(CilOpCodes.Pop));
                            index++;
                        }

                        if (msig.ReturnType.ElementType == ElementType.Boolean)
                        {
                            CIL.OpCode = CilOpCodes.Ldc_I4_0;
                            CIL.Operand = null;
                            continue;
                        }

                        if (msig.ReturnsValue)
                        {
                            if (!msig.ReturnType.IsValueType)
                            {
                                body.Instructions.Insert(index, new CilInstruction(CilOpCodes.Ldnull));
                                index++;
                            }
                        }

                        body.Instructions.RemoveAt(index);
                        index--;
                    }
                }
            }
        }
    #if DEBUG
        for (int index = 0; index < asm.Assembly.CustomAttributes.Count; index++)
        {
            CustomAttribute attr = asm.Assembly.CustomAttributes[index];
            if (
                attr.Constructor.DeclaringType.FullName == "System.Diagnostics.DebuggableAttribute" &&
                attr.Constructor.DeclaringType.DefinitionAssembly().IsCorLib)
            {
                asm.Assembly.CustomAttributes.RemoveAt(index--);
            }
        }

        TypeSignature enumRef = importer.ImportTypeSignature(typeof(DebuggableAttribute.DebuggingModes));
        CustomAttribute debugAttr = new CustomAttribute(importer.ImportType(typeof(DebuggableAttribute))
                .CreateMemberReference(".ctor",
                    MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void,
                        importer.ImportTypeSignature(typeof(DebuggableAttribute.DebuggingModes)))).ImportWith(importer),
            new CustomAttributeSignature(new CustomAttributeArgument(enumRef,
                (int)(DebuggableAttribute.DebuggingModes.DisableOptimizations |
                      DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints |
                      DebuggableAttribute.DebuggingModes.EnableEditAndContinue))));

        asm.Assembly.CustomAttributes.Add(debugAttr);
    #endif
    }
}
