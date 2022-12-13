using System.Collections;
using System.Text;

namespace ScriptSourceGenerator;

/// <summary>
/// Generated output files by csx scripts
/// </summary>
public class OutputFiles : IEnumerable<(string, StringBuilder)>
{
    private readonly Dictionary<string, StringBuilder> _outputFiles = new();

    /// <summary>
    /// Indexer to get StringBuilder of the file
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public StringBuilder this[string fileName]
    {
        get => _outputFiles.TryGetValue(fileName, out var content) ? content : (_outputFiles[fileName] = new StringBuilder()); 
    }

    /// <inheritdoc />
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