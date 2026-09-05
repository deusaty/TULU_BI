using Android.App;
using Android.Runtime;

namespace TULU_BI
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => Dashboard.CreateMauiApp();
    }
}
