using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Tests;

internal static class JsonTestHelper
{
    public static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
