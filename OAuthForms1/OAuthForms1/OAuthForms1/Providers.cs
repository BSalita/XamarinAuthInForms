using System;
using System.Collections.Generic;
using System.Reflection;
using Xamarin.Forms;

using UAuth;

namespace OAuthForms1
{
    // Json file bindings
    public class AuthProvider
    {
        // properties in common between authentication types
        public string name { get; set; }
        public string image { get; set; }
        public List<string> apiRequests { get; set; }

        // shared OAuth properties
        public string authorizeUrl { get; set; }
        public string accessTokenUrl { get; set; }

        // OAuth1 specfic properties
        public string consumerKey { get; set; }
        public string consumerSecret { get; set; }
        public string requestTokenUrl { get; set; }
        public string callbackUrl { get; set; }

        // OAuth2 specific properties
        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string scope { get; set; }
        public string redirectUrl { get; set; }

        // Urls helpful to developers to learn and use the providers APIs
        public string developerWebsite { get; set; }
        public string developerApiDocs { get; set; }
        public string developerAppConsole { get; set; }
        public string developerOAuthDocs { get; set; }
        public string developerAppRegistration { get; set; }

        // sample OAuth calls
        public string sampleAuthorizeGet { get; set; }
        public string sampleAccessTokenGet { get; set; }

        // Fixes to quirky implementations of OAuth
        public bool ForceRequestTypeOfCode { get; set; } // fix non-standard issue
        public string SubstituteRedirectUrlAccessToken { get; set; } // fix non-standard issue
        public string SubstituteRequestAccessToken { get; set; } // fix non-standard issue

        // notes
        public string comments { get; set; }
    }

    public class AuthProvidersList
    {
        public List<AuthProvider> AuthProviders { get; set; }
    }

    public class AuthProviders
    {
        public Dictionary<string, AuthProvider> AuthProviderDictionary = new Dictionary<string, AuthProvider>();
        public AuthProviders()
        {
            Type classType = typeof(AuthProvidersList); // TODO: find way of not hardcoding class name
            TypeInfo classTypeInfo = classType.GetTypeInfo();
            Assembly assemblyType = classTypeInfo.Assembly;
            foreach (var res in assemblyType.GetManifestResourceNames())
                System.Diagnostics.Debug.WriteLine("found resource: " + res);

            System.IO.Stream stream = assemblyType.GetManifestResourceStream(classType.Namespace + ".OAuthProviders.json");
            System.IO.StreamReader sr = new System.IO.StreamReader(stream);
            string json = sr.ReadToEnd();
            // TODO: implement error checking and handling of JSON
            AuthProvidersList providers = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthProvidersList>(json);
            foreach (AuthProvider ap in providers.AuthProviders)
                AuthProviderDictionary.Add(ap.name, ap);
#if true // removes saved accounts. called once on startup.
            foreach (AuthProvider ap in providers.AuthProviders)
                foreach (Account account in AccountStore.Create().FindAccountsForService(ap.name))
                    AccountStore.Create().Delete(account, ap.name);
#endif
        }
    }

    public class ProviderPage : ContentPage
    {
        public ProviderPage()
        {
            AuthProviders aps = new AuthProviders();
            Dictionary<string, AuthProvider> AuthProviders = aps.AuthProviderDictionary;

            StackLayout ProviderList = new StackLayout();
            // TODO: what to do if no valid providers?
            foreach (AuthProvider p in AuthProviders.Values)
            {
                Button b = new Button() { BackgroundColor = Color.White, HorizontalOptions = LayoutOptions.FillAndExpand};
                Image i = new Image() { BackgroundColor = Color.White, HeightRequest = 30 };
                StackLayout sl = new StackLayout { BackgroundColor = Color.White, Orientation = StackOrientation.Horizontal, Children = { i, b } };
                ProviderList.Children.Add(sl);
#if false
                // Button image is too inflexible for use. It only offers resource  files as the source and not URIs or other image forms.
                //string defaultImageFolder = Device.OnPlatform(iOS: "Images/", Android: "", WinPhone: "Images/");
                //b.Image = defaultImageFolder + (string.IsNullOrEmpty(p.image) ? "oauth.png" : p.image);
#endif
                if (string.IsNullOrEmpty(p.image))
                    i.Source = ImageSource.FromResource("OAuthForms1.oauth.jpg"); // PCL - must have "Build Action" set to "Embedded Resource". Note use of namespace.
                else
                    i.Source = ImageSource.FromUri(new Uri(p.image));
                b.Text = p.name;
                b.Clicked += (s, e) => { Navigation.PushModalAsync(new AuthenticatonPage(Auth.auth, AuthProviders[((Button)b).Text])); };
            }
            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    new ScrollView {
                        Content =
                        ProviderList
                        }
                }
            };
        }
    }
}