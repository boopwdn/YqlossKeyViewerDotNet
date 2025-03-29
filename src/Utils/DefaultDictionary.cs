namespace YqlossKeyViewerDotNet.Utils;

public class DefaultDictionary<TAccessKey, TMapKey, TValue> where TMapKey : notnull
{
    public delegate TMapKey KeyTransformerFunc(TAccessKey key);

    public delegate TValue DefaultValueFunc(TAccessKey accessKey, TMapKey mapKey);

    public required KeyTransformerFunc KeyTransformer { get; init; }
    public required DefaultValueFunc DefaultValue { get; init; }

    public Dictionary<TMapKey, TValue> Data { get; } = [];

    public TValue this[TAccessKey accessKey]
    {
        get
        {
            var mapKey = KeyTransformer(accessKey);
            if (Data.TryGetValue(mapKey, out var value)) return value;
            return Data[mapKey] = DefaultValue(accessKey, mapKey);
        }
        set => Data[KeyTransformer(accessKey)] = value;
    }
}