using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace CentersBarCode.Platforms.Android;

// Add this activity to handle redirect URL for Google authentication
[Activity(Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataHost = "auth",
    DataScheme = "msal613797922873-vmu2lko85b1mkv0lno9is9d557ggog4o.apps.googleusercontent.com")]
public class MsalActivity : BrowserTabActivity
{
}