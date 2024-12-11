using DiffEngine;
using System.Runtime.CompilerServices;

namespace ScriptSourceGenerator.Test;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        DiffTools.UseOrder(DiffTool.VisualStudio, DiffTool.WinMerge, DiffTool.VisualStudioCode);
        VerifySourceGenerators.Initialize();
    }
}
