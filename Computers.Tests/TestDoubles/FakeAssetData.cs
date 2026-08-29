using StardewModdingAPI;

namespace Computers.Tests.TestDoubles;

public class FakeAssetData : IAssetData {
    public FakeAssetData(string name, object data) {
        Name = FakeAssetName.Of(name);
        Data = data;
    }

    public object Data { get; private set; }
    public string? Locale => null;
    public IAssetName Name { get; }
    public IAssetName NameWithoutLocale => Name;
    public Type DataType => Data.GetType();

    public TData GetData<TData>() => (TData) Data;

    public void ReplaceWith(object value) => Data = value;

    public IAssetDataForDictionary<TKey, TValue> AsDictionary<TKey, TValue>() where TKey : notnull =>
        throw new NotSupportedException();

    public IAssetDataForImage AsImage() => throw new NotSupportedException();

    public IAssetDataForMap AsMap() => throw new NotSupportedException();
}
