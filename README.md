# XdtHtml

This package enables an equivalent to dotnet-xdt transformations for html files.
Please review documentation on syntax at: https://github.com/nil4/dotnet-transform-xdt and https://docs.microsoft.com/en-us/previous-versions/dd465326(v=vs.100)

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