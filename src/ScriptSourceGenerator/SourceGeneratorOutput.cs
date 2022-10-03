using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ScriptSourceGenerator;

public class OutputFiles : IEnumerable<(string, StringBuilder)>
{
    private readonly Dictionary<string, StringBuilder> _outputFiles = new();

    public StringBuilder this[string fileName]
    {
        get => _outputFiles.TryGetValue(fileName, out var content) ? content : (_outputFiles[fileName] = new StringBuilder()); 
    }

    public IEnumerator<(string, StringBuilder)> GetEnumerator()
    {
        foreach (var kv in _outputFiles)
        {
            yield return (kv.Key, kv.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var kv in _outputFiles)
        {
            yield return (kv.Key, kv.Value);
        }
    }
}