using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace CentersBarCode.Platforms.Android;

// Add this activity to handle redirect URL for Google authentication
[Activity(Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataHost = "auth",
    DataScheme = "219293182439-0erlvl0k2cba8afuumgi76t70sm6g9tc.apps.googleusercontent.com")]
public class MsalActivity : BrowserTabActivity
{
}
