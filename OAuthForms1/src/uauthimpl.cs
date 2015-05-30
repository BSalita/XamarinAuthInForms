using System;
using System.Collections.Generic;
#if SILVERLIGHT
using System.Windows.Navigation;
#endif
using UAuth;

namespace UAuthImpl
{
    public class Auth : IAuth
    {
        public IOAuth1Authenticator auth1 { get; set; }
        public IOAuth2Authenticator auth2 { get; set; }
#if __ANDROID__
        public static Android.App.Activity context;
        public Auth(Android.App.Activity pContext)
        {
            context = pContext;
#elif __IOS__
        public static MonoTouch.Dialog.DialogViewController dialog; // not sure about static
        public Auth()
        {
#elif SILVERLIGHT
        public Auth()
        {
#endif
            new AccountStoreImpl(); // invoke static constructor
            auth1 = new OAuth1AuthenticatorImpl();
            auth2 = new OAuth2AuthenticatorImpl();
        }
    }

    public class BaseOAuth2Authenticator : Xamarin.Auth.OAuth2Authenticator
    {
        string replacementUrlFormat;
        public BaseOAuth2Authenticator(string clientId, string scope, Uri authorizeUrl, Uri redirectUrl) : base(clientId, scope, authorizeUrl, redirectUrl)
        {
            this.replacementUrlFormat = null;
        }

        public BaseOAuth2Authenticator(string clientId, string scope, Uri authorizeUrl, Uri redirectUrl, string replacementUrlFormat) : base(clientId, scope, authorizeUrl, redirectUrl)
        {
            this.replacementUrlFormat = replacementUrlFormat;
        }

        public BaseOAuth2Authenticator(string clientId, string clientSecret, string scope, Uri authorizeUrl, Uri redirectUrl, Uri accessTokenUrl, GetUsernameAsyncFunc getUsernameAsync = null) : base(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl)
        {
            this.replacementUrlFormat = null; // replacementUrlFormat not implemented
        }

        protected async override void OnRedirectPageLoaded(Uri url, IDictionary<string, string> query, IDictionary<string, string> fragment)
        {
            System.Diagnostics.Debug.WriteLine("OnRedirectPageLoaded url:" + url);
            foreach (KeyValuePair<string, string> p in query)
                System.Diagnostics.Debug.WriteLine("query: Key:" + p.Key + " Value:" + p.Value);
            foreach (KeyValuePair<string, string> p in fragment)
                System.Diagnostics.Debug.WriteLine("fragment: Key:" + p.Key + " Value:" + p.Value);
            await System.Threading.Tasks.Task.Delay(2000); // TODO: find another way to pause on redirect page.
                                                           // Fixes SubstituteRedirectUrlAccessToken issue but just do for every site
            if (!fragment.Keys.Contains("access_token") && query.Keys.Contains("code")) // fixes missing access_token: GitHub, LinkedIn
                fragment.Add("access_token", query["code"]);
            base.OnRedirectPageLoaded(url, query, fragment);
        }

        public async override System.Threading.Tasks.Task<Uri> GetInitialUrlAsync()
        {
            // just return if no replacement requested
            if (string.IsNullOrEmpty(replacementUrlFormat))
                return await base.GetInitialUrlAsync();

            // get base class Uri
            System.Diagnostics.Debug.WriteLine("GetUriFromTaskUri: replacementUrlFormat:" + replacementUrlFormat);
            Uri uri = await base.GetInitialUrlAsync();
            System.Diagnostics.Debug.WriteLine("GetUriFromTaskUri: base.uri:" + uri);

            // need to extract state query string from base Uri because its scope isn't public.
            string baseUrl = uri.OriginalString;
            int stateIndex = baseUrl.LastIndexOf("&state=");
            string requestState = baseUrl.Substring(stateIndex + "&state=".Length);

            // verify that the base Url is same as our supposedly identical procedure. If not, there must be a code change in a new version of base class.
            string redoUrl = string.Format(
                             "{0}?client_id={1}&redirect_uri={2}&response_type={3}&scope={4}&state={5}",
                            AuthorizeUrl.AbsoluteUri,
                            Uri.EscapeDataString(ClientId),
                            Uri.EscapeDataString(RedirectUrl.AbsoluteUri),
                            AccessTokenUrl == null ? "token" : "code",
                            Uri.EscapeDataString(Scope),
                            Uri.EscapeDataString(requestState));
            if (baseUrl != redoUrl)
                throw new ArgumentException("GetInitialUrlAsync: Url comparison failure: base: " + baseUrl + " redo:" + redoUrl);

            // format replacement Uri
            uri = new Uri(string.Format(
                            replacementUrlFormat,
                            AuthorizeUrl.AbsoluteUri,
                            Uri.EscapeDataString(ClientId),
                            Uri.EscapeDataString(RedirectUrl.AbsoluteUri),
                            AccessTokenUrl == null ? "token" : "code",
                            Uri.EscapeDataString(Scope),
                            Uri.EscapeDataString(requestState)));
            System.Diagnostics.Debug.WriteLine("GetUriFromTaskUri: replacement uri:" + uri);

            System.Threading.Tasks.TaskCompletionSource<Uri> tcs = new System.Threading.Tasks.TaskCompletionSource<Uri>();
            tcs.SetResult(uri);
            return uri;
        }
    }

    public class OAuth1AuthenticatorImpl : IOAuth1Authenticator
    {
        public bool AllowCancel;
        public event EventHandler<AuthenticatorCompletedEventArgs> Completed;
        public event EventHandler<AuthenticatorErrorEventArgs> Error;
        private static readonly System.Threading.Tasks.TaskScheduler UIScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();

        public void OAuth1Authenticator(string consumerKey, string consumerSecret, Uri requestTokenUrl, Uri authorizeUrl, Uri accessTokenUrl, Uri callbackUrl, GetUsernameAsyncFunc getUsernameAsync)
        {
            Xamarin.Auth.OAuth1Authenticator auth1 = new Xamarin.Auth.OAuth1Authenticator(consumerKey, consumerSecret, requestTokenUrl, authorizeUrl, accessTokenUrl, callbackUrl, null); // TODO: getUsernameAsync argument not implemented
            auth1.AllowCancel = AllowCancel;
            auth1.Completed += (sender, eventArgs) =>
            {
                Completed(auth1, new AuthenticatorCompletedEventArgs(new Account(eventArgs.Account, eventArgs.Account.Properties, eventArgs.Account.Username)));
            };
            auth1.Error += (sender, eventArgs) =>
            {
                Error(sender, new AuthenticatorErrorEventArgs(eventArgs.Message, eventArgs.Exception));
            };
#if __ANDROID__
            Android.Content.Intent intent = auth1.GetUI(Auth.context);
            Auth.context.StartActivity(intent);
#elif __IOS__
            UIKit.UIViewController vc = auth1.GetUI();
	        Auth.dialog.PresentViewController(vc, true, null);
#elif SILVERLIGHT
            Uri uri = auth1.GetUI();
	        NavigationService.Navigate(uri);
#endif
        }

        public IOAuth1Request OAuth1Request(string method, Uri url, Dictionary<string, string> parameters, Account account, bool includeMultipartInSignature = false)
        {
            Xamarin.Auth.OAuth1Request xrequest = new Xamarin.Auth.OAuth1Request(method, url, parameters, (Xamarin.Auth.Account)account.xAccount, includeMultipartInSignature);
            return new OAuth1RequestImpl(xrequest);
        }
    }

    public class OAuth1RequestImpl : IOAuth1Request
    {
        public Xamarin.Auth.OAuth1Request xrequest;
        public OAuth1RequestImpl(Xamarin.Auth.OAuth1Request xrequest)
        {
            this.xrequest = xrequest;
        }

        public System.Threading.Tasks.Task<IResponse> GetResponseAsync()
        {
            System.Threading.Tasks.TaskCompletionSource<IResponse> tcs = new System.Threading.Tasks.TaskCompletionSource<IResponse>();
            tcs.SetResult(new ResponseImpl(xrequest.GetResponseAsync()));
            return tcs.Task;
        }

        public System.Threading.Tasks.Task<IResponse> GetResponseAsync(System.Threading.CancellationToken cancellationToken)
        {
            System.Threading.Tasks.TaskCompletionSource<IResponse> tcs = new System.Threading.Tasks.TaskCompletionSource<IResponse>();
            tcs.SetResult(new ResponseImpl(xrequest.GetResponseAsync(cancellationToken)));
            return tcs.Task;
        }
    }

    public class OAuth2AuthenticatorImpl : IOAuth2Authenticator
    {
        public bool AllowCancel;
        public event EventHandler<AuthenticatorCompletedEventArgs> Completed;
        public event EventHandler<AuthenticatorErrorEventArgs> Error;
        private static readonly System.Threading.Tasks.TaskScheduler UIScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();

        public void OAuth2Authenticator(string clientId, string scope, Uri authorizeUrl, Uri redirectUrl)
        {
            Xamarin.Auth.OAuth2Authenticator auth2 = new BaseOAuth2Authenticator(clientId, scope, authorizeUrl, redirectUrl);
            auth2.AllowCancel = AllowCancel;
            auth2.Completed += (sender, eventArgs) =>
            {
                Completed(auth2, new AuthenticatorCompletedEventArgs(new Account(eventArgs.Account, eventArgs.Account.Properties, eventArgs.Account.Username)));
            };
            auth2.Error += (sender, eventArgs) =>
            {
                Error(sender, new AuthenticatorErrorEventArgs(eventArgs.Message, eventArgs.Exception));
            };
#if __ANDROID__
            Android.Content.Intent intent = auth2.GetUI(Auth.context);
            Auth.context.StartActivity(intent);
#elif __IOS__
            UIKit.UIViewController vc = auth2.GetUI();
	        Auth.dialog.PresentViewController(vc, true, null);
#elif SILVERLIGHT
            Uri uri = auth2.GetUI();
	        NavigationService.Navigate(uri);
#endif
        }

        public void OAuth2Authenticator(string clientId, string scope, Uri authorizeUrl, Uri redirectUrl, string replacementFormatUrl)
        {
            Xamarin.Auth.OAuth2Authenticator auth2 = new BaseOAuth2Authenticator(clientId, scope, authorizeUrl, redirectUrl, replacementFormatUrl);
            auth2.AllowCancel = AllowCancel;
            auth2.Completed += (sender, eventArgs) =>
            {
                Completed(auth2, new AuthenticatorCompletedEventArgs(new Account(eventArgs.Account, eventArgs.Account.Properties, eventArgs.Account.Username)));
            };
            auth2.Error += (sender, eventArgs) =>
            {
                Error(sender, new AuthenticatorErrorEventArgs(eventArgs.Message, eventArgs.Exception));
            };
#if __ANDROID__
            Android.Content.Intent intent = auth2.GetUI(Auth.context);
            Auth.context.StartActivity(intent);
#elif __IOS__
            UIKit.UIViewController vc = auth2.GetUI();
	        Auth.dialog.PresentViewController(vc, true, null);
#elif SILVERLIGHT
            Uri uri = auth2.GetUI();
	        NavigationService.Navigate(uri);
#endif
        }

        public void OAuth2Authenticator(string clientId, string clientSecret, string scope, Uri authorizeUrl, Uri redirectUrl, Uri accessTokenUrl, GetUsernameAsyncFunc getUsernameAsync = null)
        {
            Xamarin.Auth.OAuth2Authenticator auth2 = new BaseOAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null); // TODO: getUsernameAsync argument not implemented
            auth2.AllowCancel = AllowCancel;
            auth2.Completed += (sender, eventArgs) =>
            {
                Completed(auth2, new AuthenticatorCompletedEventArgs(new Account(eventArgs.Account, eventArgs.Account.Properties, eventArgs.Account.Username)));
            };
#if __ANDROID__
            Android.Content.Intent intent = auth2.GetUI(Auth.context);
            Auth.context.StartActivity(intent);
#elif __IOS__
            UIKit.UIViewController vc = auth2.GetUI();
	        Auth.dialog.PresentViewController(vc, true, null);
#elif SILVERLIGHT
            Uri uri = auth2.GetUI();
	        NavigationService.Navigate(uri);
#endif
        }

        public IOAuth2Request OAuth2Request(string method, Uri url, Dictionary<string, string> parameters, Account account)
        {
            Xamarin.Auth.OAuth2Request xrequest = new Xamarin.Auth.OAuth2Request(method, url, parameters, (Xamarin.Auth.Account)account.xAccount);
            return new OAuth2RequestImpl(xrequest);
        }
    }

    public class OAuth2RequestImpl : IOAuth2Request
    {
        public Xamarin.Auth.OAuth2Request xrequest;

        public OAuth2RequestImpl(Xamarin.Auth.OAuth2Request xrequest)
        {
            this.xrequest = xrequest;
        }

        public System.Threading.Tasks.Task<IResponse> GetResponseAsync()
        {
            System.Threading.Tasks.TaskCompletionSource<IResponse> tcs = new System.Threading.Tasks.TaskCompletionSource<IResponse>();
            tcs.SetResult(new ResponseImpl(xrequest.GetResponseAsync()));
            return tcs.Task;
        }

        public System.Threading.Tasks.Task<IResponse> GetResponseAsync(System.Threading.CancellationToken cancellationToken)
        {
            System.Threading.Tasks.TaskCompletionSource<IResponse> tcs = new System.Threading.Tasks.TaskCompletionSource<IResponse>();
            tcs.SetResult(new ResponseImpl(xrequest.GetResponseAsync(cancellationToken)));
            return tcs.Task;
        }

        public string AccessTokenParameterName { get { return xrequest.AccessTokenParameterName; } set { xrequest.AccessTokenParameterName = value; } }
    }

    public class ResponseImpl : IResponse
    {
        public System.Threading.Tasks.Task<Xamarin.Auth.Response> xresponse;
        public ResponseImpl(System.Threading.Tasks.Task<Xamarin.Auth.Response> xresponse)
        {
            this.xresponse = xresponse;
        }
        public IDictionary<string, string> Headers { get { xresponse.Wait(); return xresponse.Result.Headers; } }
        public Uri ResponseUri { get { xresponse.Wait(); return xresponse.Result.ResponseUri; } }
        public int StatusCode { get { xresponse.Wait(); return (int)xresponse.Result.StatusCode; } } // should be Net.HttpStatusCode
                                                                                                     //public IO.Stream GetResponseStream() { return xresponse.Result.GetResponseStream(); } // not implemented
        public string GetResponseText() { xresponse.Wait(); return xresponse.Result.GetResponseText(); }
    }

    public class AccountStoreImpl : IAccountStore
    {
        Xamarin.Auth.AccountStore xAccountStore;
        static AccountStoreImpl()
        {
            AccountStore.xAccountStore = new AccountStoreImpl();
        }
        public IAccountStore xCreate()
        {
#if __ANDROID__
            xAccountStore = Xamarin.Auth.AccountStore.Create(Auth.context); // context may be all wrong
#elif __IOS__
            xAccountStore = Xamarin.Auth.AccountStore.Create();
#elif SILVERLIGHT
            xAccountStore = Xamarin.Auth.AccountStore.Create();
#endif
            return this;
        }
        public void Delete(Account account, string serviceId)
        {
            xAccountStore.Delete((Xamarin.Auth.Account)account.xAccount, serviceId);
        }
        public List<Account> FindAccountsForService(string serviceId)
        {
            IEnumerable<Xamarin.Auth.Account> xaccounts = xAccountStore.FindAccountsForService(serviceId);
            List<Account> accounts = new List<Account>();
            foreach (Xamarin.Auth.Account xaccount in xaccounts)
                accounts.Add(new Account(xaccount, xaccount.Properties, xaccount.Username));
            return accounts;
        }
        public void Save(Account account, string serviceId)
        {
            xAccountStore.Save((Xamarin.Auth.Account)account.xAccount, serviceId);
        }
    }
}