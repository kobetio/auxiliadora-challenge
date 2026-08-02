using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentalPipeline.IntegrationTests.Infrastructure;

/// <summary>
/// The API serializes enums as strings via a <see cref="JsonStringEnumConverter"/> registered in
/// <c>Program.cs</c>'s <c>AddJsonOptions</c>. That configuration only applies to ASP.NET Core's own
/// MVC (de)serialization pipeline, not to test code calling <c>PostAsJsonAsync</c>/<c>ReadFromJsonAsync</c>
/// on a plain <see cref="HttpClient"/>, so tests need this equivalent, explicit options instance to
/// read/write the same JSON shape (e.g. <c>"Available"</c> instead of <c>0</c>) the real API produces.
/// </summary>
public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
