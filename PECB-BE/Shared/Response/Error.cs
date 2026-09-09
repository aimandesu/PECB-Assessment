using System.Net;
using System.Text.Json.Serialization;

namespace PECB_BE.Shared.Response;

public sealed record Error(
    [property: JsonConverter(typeof(JsonNumberEnumConverter<HttpStatusCode>))]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    HttpStatusCode? Status,
    string Description,
    object? CustomObject = null);