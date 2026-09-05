using Foundation;

namespace TULU_BI
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => Dashboard.CreateMauiApp();
    }
}
