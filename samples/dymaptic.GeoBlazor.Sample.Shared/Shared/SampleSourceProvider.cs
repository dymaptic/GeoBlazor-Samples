using System.Reflection;

namespace dymaptic.GeoBlazor.Core.Sample.Shared.Shared;

public interface ISampleSourceProvider
{
    (string Razor, string? CodeBehind) GetSource(Type pageType);
}

public class SampleSourceProvider : ISampleSourceProvider
{
    public (string Razor, string? CodeBehind) GetSource(Type pageType)
    {
        Assembly assembly = pageType.Assembly;
        string razor = ReadResource(assembly, $"SampleSource/{pageType.Name}.razor") ?? string.Empty;
        string? codeBehind = ReadResource(assembly, $"SampleSource/{pageType.Name}.razor.cs");
        return (razor, codeBehind);
    }

    private static string? ReadResource(Assembly assembly, string logicalName)
    {
        using Stream? stream = assembly.GetManifestResourceStream(logicalName);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
