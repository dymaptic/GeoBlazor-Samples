using UIKit;
using Uno.UI.Hosting;

namespace FieldAssetInspector.MacCatalyst;

public class EntryPoint
{
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();
        host.Run();
    }
}
