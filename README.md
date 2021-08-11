# XdtHtml

This package enables an equivalent to dotnet-xdt transformations for html files.
Please review documentation on syntax at: https://github.com/dotnet/xdt and https://msdn.microsoft.com/en-us/library/dd465326.aspx

Sample code:
```csharp
var x = new HtmlTransformableDocument();
x.LoadHtml(fileContent);

using (var transform = new HtmlTransformation(translationFilePath))
{
    if (transform.Apply(x))
    {
        return x.DocumentNode.OuterHtml;
    }
}
```