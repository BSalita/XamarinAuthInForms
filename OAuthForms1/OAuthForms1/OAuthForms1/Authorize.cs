using System;
using System.Collections.Generic;
using Xamarin.Forms;

using UAuth;

namespace OAuthForms1
{
    public class AuthenticatonPage : ContentPage
    {
        public static readonly BindableProperty RequestTextProperty = BindableProperty.Create<AuthenticatonPage, string>(p => p.RequestText, string.Empty);
        public string RequestText { set { SetValue(RequestTextProperty, value); } get { return (string)GetValue(RequestTextProperty); } }
        public static readonly BindableProperty ResponseTextProperty = BindableProperty.Create<AuthenticatonPage, string>(p => p.ResponseText, string.Empty);
        public string ResponseText { set { SetValue(ResponseTextProperty, value); } get { return (string)GetValue(ResponseTextProperty); } }
        public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create<AuthenticatonPage, string>(p => p.ErrorText, string.Empty);
        public string ErrorText { set { SetValue(ErrorTextProperty, value); } get { return (string)GetValue(ErrorTextProperty); } }

        public AuthenticatonPage(IAuth auth, AuthProvider ap)
        {
            // bind message to controls on page
            Label RequestLabel = new Label { XAlign = TextAlignment.Center, BindingContext = this };
            RequestLabel.SetBinding(Label.TextProperty, nameof(RequestText));
            Label ResponseLabel = new Label { XAlign = TextAlignment.Center, BindingContext = this };
            ResponseLabel.SetBinding(Label.TextProperty, nameof(ResponseText));
            Label ErrorLabel = new Label { XAlign = TextAlignment.Center, BindingContext = this };
            ErrorLabel.SetBinding(Label.TextProperty, nameof(ErrorText));

            Content = new StackLayout
            {
                Children = {
                    new StackLayout // TODO: the display of request/response/error messages is really lame. Need help with better UI.
                    {
                        Children =
                        {
                            new Label { Text = "Request Info", HorizontalOptions = LayoutOptions.CenterAndExpand, BackgroundColor = Color.Blue },
                            new ScrollView {
                                HeightRequest = 150,
                                Content =
                                RequestLabel
                                }
                       }
                    },
                    new StackLayout
                    {
                        Children =
                        {
                            new Label { Text = "Response Info", HorizontalOptions = LayoutOptions.CenterAndExpand, BackgroundColor = Color.Blue },
                            new ScrollView {
                                HeightRequest = 150,
                                Content =
                                ResponseLabel
                                }
                       }
                    },
                    new StackLayout
                    {
                        Children =
                        {
                            new Label { Text = "Error Info", HorizontalOptions = LayoutOptions.CenterAndExpand, BackgroundColor = Color.Blue },
                            new ScrollView {
                                HeightRequest = 150,
                                Content =
                                ErrorLabel
                                }
                       },
                   }
                }
            };
            if (!string.IsNullOrEmpty(ap.consumerKey))
            {
                Authenticate(auth.auth1, ap, AccountStore.Create().FindAccountsForService(ap.name));
            }
            else if (!string.IsNullOrEmpty(ap.clientId))
            {
                Authenticate(auth.auth2, ap, AccountStore.Create().FindAccountsForService(ap.name));
            }
            else
            {
                ErrorText = "Authenticator: Empty consumerKey (OAuth1) and clientId (OAuth2)";
            }
        }

        void Authenticate(IOAuth1Authenticator auth, AuthProvider ap, List<Account> accounts)
        {
            if (accounts.Count == 0)
            {
                auth.Completed += (sender, eventArgs) =>
                {
                    if (eventArgs.IsAuthenticated)
                    {
                        try
                        {
                            AccountStore.Create().Save(eventArgs.Account, ap.name);
                            PerformAuth1TestRequests(ap, eventArgs.Account);
                        }
                        catch (Exception ex)
                        {
                            ErrorText = ex.Message;
                        }
                    }
                    else
                    {
                        ErrorText = "Authenticate: Not Authenticated";
                        return;
                    }
                };
                auth.Error += (sender, eventArgs) =>
                {
                    ErrorText += "Authenticate: Error:" + eventArgs.Message + "\n";
                    for (Exception inner = eventArgs.Exception; inner != null; inner = inner.InnerException)
                    {
                        ErrorText += "Message:" + inner.Message + "\n";
                    }
                    return;
                };
                try
                {
                    auth.OAuth1Authenticator(ap.consumerKey, ap.consumerSecret, new Uri(ap.requestTokenUrl), new Uri(ap.authorizeUrl), new Uri(ap.accessTokenUrl), new Uri(ap.callbackUrl), null);
                }
                catch (Exception ex)
                {
                    ErrorText += "Authenticate: Exception:";
                    for (Exception inner = ex.InnerException; inner != null; inner = inner.InnerException)
                    {
                        ErrorText += "Message:" + inner.Message + "\n";
                    }
                }
            }
            else
                PerformAuth1TestRequests(ap, accounts[0]); // TODO: implement error handling. If error is caused by expired token, renew token.
        }

        async void PerformAuth1TestRequests(AuthProvider ap, Account account)
        {
            foreach (KeyValuePair<string, string> p in account.Properties)
                System.Diagnostics.Debug.WriteLine("Property: Key:" + p.Key + " Value:" + p.Value);
            try
            {
                foreach (string requestUrl in ap.apiRequests)
                {
                    OAuth1Request request = new OAuth1Request("GET", new Uri(requestUrl), null, account, false);
                    IResponse response = await request.GetResponseAsync();
                    ResponseText = response.GetResponseText();
                }
            }
            catch (Exception ex)
            {
                ErrorText = "PerformAuth1TestRequests: Exception:";
                for (Exception inner = ex; inner != null; inner = inner.InnerException)
                {
                    ErrorText += "Message:" + inner.Message + "\n";
                }
                foreach (KeyValuePair<string, string> p in account.Properties)
                    ErrorText += "Key:" + p.Key + " Value:" + p.Value + "\n";
            }
        }

        void Authenticate(IOAuth2Authenticator auth, AuthProvider ap, List<Account> accounts)
        {
            if (accounts.Count == 0)
            {
                auth.Completed += (sender, eventArgs) =>
                {
                    if (eventArgs.IsAuthenticated)
                    {
                        try
                        {
                            AccountStore.Create().Save(eventArgs.Account, ap.name);
                            PerformAuth2TestRequests(ap, eventArgs.Account);
                        }
                        catch (Exception ex)
                        {
                            ErrorText = ex.Message;
                        }
                    }
                    else
                    {
                        ErrorText = "Authenticate: Not Authenticated";
                        return;
                    }
                };
                auth.Error += (sender, eventArgs) =>
                {
                    ErrorText += "Authenticate: Error:" + eventArgs.Message + "\n";
                    Exception ex = eventArgs.Exception;
                    for (Exception inner = eventArgs.Exception; inner != null; inner = inner.InnerException)
                    {
                        ErrorText += "Message:" + inner.Message + "\n";
                    }
                    return;
                };
                try
                {
                    if (ap.ForceRequestTypeOfCode)
                    {
                        auth.OAuth2Authenticator(ap.clientId, ap.scope, new Uri(ap.authorizeUrl), new Uri(ap.redirectUrl), "{0}?client_id={1}&redirect_uri={2}&response_type=code&scope={4}&state={5}");
                    }
                    else
                    {
                        auth.OAuth2Authenticator(ap.clientId, ap.scope, new Uri(ap.authorizeUrl), new Uri(ap.redirectUrl));
                    }
                }
                catch (Exception ex)
                {
                    ErrorText = "Authenticate: Exception:";
                    for (Exception inner = ex.InnerException; inner != null; inner = inner.InnerException)
                    {
                        ErrorText += "Message:" + inner.Message + "\n";
                    }
                    foreach (KeyValuePair<string, string> p in accounts[0].Properties)
                        ErrorText += "Key:" + p.Key + " Value:" + p.Value + "\n";
                }
            }
            else
                PerformAuth2TestRequests(ap, accounts[0]); // TODO: implement error handling. If error is caused by expired token, renew token.
        }

        async void PerformAuth2TestRequests(AuthProvider ap, Account account)
        {
            try
            {
                ResponseText = ""; // clear response display string
                foreach (KeyValuePair<string, string> p in account.Properties)
                    System.Diagnostics.Debug.WriteLine("Property: Key:" + p.Key + " Value:" + p.Value);
                System.Diagnostics.Debug.WriteLine("PerformAuth2TestRequests: Count:" + ap.apiRequests.Count);
                foreach (string requestUrl in ap.apiRequests)
                {
                    System.Diagnostics.Debug.WriteLine("PerformAuth2TestRequests: Url:" + requestUrl);
                    ResponseText += "Request Url:" + requestUrl + "\n";
                    OAuth2Request request = new OAuth2Request("GET", new Uri(requestUrl), null, account);
                    if (!string.IsNullOrEmpty(ap.SubstituteRequestAccessToken))
                        request.AccessTokenParameterName = ap.SubstituteRequestAccessToken;
                    IResponse response = await request.GetResponseAsync();
                    System.Diagnostics.Debug.WriteLine("PerformAuth2TestRequests: StatusCode:" + response.StatusCode + " ResponseUri:" + response.ResponseUri);
                    System.Diagnostics.Debug.WriteLine("PerformAuth2TestRequests: Headers:");
                    foreach (KeyValuePair<string, string> h in response.Headers)
                        System.Diagnostics.Debug.WriteLine("Header: Key:" + h.Key + " Value:" + h.Value);
                    ResponseText += "Response(" + response.StatusCode + "):";
                    string r = response.GetResponseText();
                    ResponseText += r + "\n";
                }
            }
            catch (Exception ex)
            {
                ErrorText += "Exception: PerformAuth2TestRequests: Message:" + ex.Message + "\n";
                foreach (KeyValuePair<string, string> p in account.Properties)
                    ErrorText += "Key:" + p.Key + " Value:" + p.Value + "\n";
            }
        }

        string HostToDomain(string host)
        {
            System.Diagnostics.Debug.WriteLine("HostToDomain: host:" + host);
            Uri uri = new Uri(host);
            string[] hostParts = uri.Host.Split('.');
            int l = hostParts.Length;
            return hostParts[l - 2] + "." + hostParts[l - 1];
        }
    }
}
