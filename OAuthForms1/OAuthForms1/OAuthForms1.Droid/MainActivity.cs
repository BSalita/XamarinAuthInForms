using System;

using Android.App;
using Android.Content.PM;
using Android.OS;

using OAuthForms1;

namespace OAuthForms1.Droid
{
    [Activity(Label = "OAuthForms1", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);
            UAuth.Auth.auth = new UAuthImpl.Auth(this);
            LoadApplication(new App());
        }
    }
}

