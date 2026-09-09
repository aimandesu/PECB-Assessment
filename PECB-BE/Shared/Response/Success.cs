using System.Text.Json.Serialization;

namespace PECB_BE.Shared.Response;

public sealed record Success<T>(
    T Data,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
    string? Description = null);