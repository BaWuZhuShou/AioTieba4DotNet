namespace AioTieba4DotNet.Models.Contents;

public sealed class FragUnknown : IFrag
{
    public string Type { get; init; } = string.Empty;

    public override string Text { get; init; } = string.Empty;

    public override string GetFragType()
    {
        return nameof(FragUnknown);
    }

    public override Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { ["type"] = Type, ["text"] = Text };
    }

    public override string ToString()
    {
        return $"{GetFragType()} {nameof(Type)}: {Type}, {nameof(Text)}: {Text}";
    }
}
