using System;
using Xamarin.Forms;

namespace OAuthForms1
{
    public partial class DoSomeWork : ContentPage
    {
        public DoSomeWork()
        {
            InitializeComponent();
        }
        void OnShowAuthenticationProviders(object sender, EventArgs eventArgs)
        {
            Navigation.PushModalAsync(new ProviderPage());
        }
    }
}
