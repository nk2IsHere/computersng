namespace Computers.Core;

[Serializable]
public record Id(List<string> Parts) {
    public const string Separator = ".";

    public string Value => string.Join(Separator, Parts);
    
    public override string ToString() => Value;
    
    public static Id Parse(string value) {
        var parts = value.Split(Separator);
        return new Id(parts.ToList());
    }

    public static Id Random(int length = 6) {
        var guidString = Guid.NewGuid().ToString("N");
        return new Id(new List<string> { guidString[..length] });
    }

    public static Id operator /(Id id, string part) {
        var parts = id.Parts.ToList();
        parts.Add(part);
        return new Id(parts);
    }
    
    public static Id operator /(Id id, Id other) {
        var parts = id.Parts.ToList();
        parts.AddRange(other.Parts);
        return new Id(parts);
    }
    
    public static implicit operator Id(string parts) => Parse(parts);
    
    public static implicit operator string(Id id) => id.Value;
    
    public static implicit operator Id(List<string> parts) => new(parts);
    
    public static implicit operator List<string>(Id id) => id.Parts;

    public virtual bool Equals(Id? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Parts.SequenceEqual(other.Parts);
    }

    public override int GetHashCode() {
        return Parts.GetSequenceHashCode();
    }
}

public static class IdExtensions {
    public static Id AsId(this string parts) => Id.Parse(parts);
}
