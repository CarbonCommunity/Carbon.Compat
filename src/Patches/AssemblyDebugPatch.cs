using System.Diagnostics;
using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

public class AssemblyDebugPatch : IAssemblyPatch
{
    public void Apply(ModuleDefinition assembly, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            foreach (var method in type.Methods)
            {
                var body = method.CilMethodBody;

                if (body == null)
                {
	                continue;
                }

                for (int i = 0; i < body.Instructions.Count; i++)
                {
                    var CIL = body.Instructions[i];

                    if (CIL.OpCode == CilOpCodes.Call && CIL.Operand is MemberReference mref && mref.DeclaringType.DefinitionAssembly().IsCorLib &&
                        mref.Signature is MethodSignature msig && (( mref.DeclaringType.Name == "Debugger" && ( mref.Name == "get_IsAttached" || mref.Name == "IsLogging" )) ||
                                                                   ( mref.DeclaringType.Name == "Environment" && mref.Name == "FailFast" ) ) )
                    {
                        for (int pc = 0; pc < msig.ParameterTypes.Count; pc++)
                        {
                            body.Instructions.Insert(i, new CilInstruction(CilOpCodes.Pop));
                            i++;
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
                                body.Instructions.Insert(i, new CilInstruction(CilOpCodes.Ldnull));
                                i++;
                            }
                        }

                        body.Instructions.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

    #if DEBUG
        for (int index = 0; index < assembly.Assembly.CustomAttributes.Count; index++)
        {
            var attr = assembly.Assembly.CustomAttributes[index];

            if (attr.Constructor.DeclaringType.FullName == "System.Diagnostics.DebuggableAttribute" && attr.Constructor.DeclaringType.DefinitionAssembly().IsCorLib)
            {
                assembly.Assembly.CustomAttributes.RemoveAt(index--);
            }
        }

        var enumRef = importer.ImportTypeSignature(typeof(DebuggableAttribute.DebuggingModes));
        var debugAttr = new CustomAttribute(importer.ImportType(typeof(DebuggableAttribute))
                .CreateMemberReference(".ctor",
                    MethodSignature.CreateInstance(assembly.CorLibTypeFactory.Void,
                        importer.ImportTypeSignature(typeof(DebuggableAttribute.DebuggingModes)))).ImportWith(importer),
            new CustomAttributeSignature(new CustomAttributeArgument(enumRef,
                (int)(DebuggableAttribute.DebuggingModes.DisableOptimizations |
                      DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints |
                      DebuggableAttribute.DebuggingModes.EnableEditAndContinue))));

        assembly.Assembly.CustomAttributes.Add(debugAttr);
    #endif
    }
}
