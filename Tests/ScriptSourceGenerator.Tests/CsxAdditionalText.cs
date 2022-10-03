using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ScriptSourceGenerator.Test;

internal class CsxAdditionalText : AdditionalText
{
    private readonly string _path;
    private readonly string _content;

    public CsxAdditionalText(string path, string content)
    {
        _content = content;
        _path = path;
    }

    public override string Path => _path;

    public override SourceText? GetText(CancellationToken cancellationToken = default) => SourceText.From(_content);
}
