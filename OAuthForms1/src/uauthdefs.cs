using System;
using System.Collections.Generic;

namespace UAuth
{
    public static class Auth // globals
    {
        public static IAuth auth;
    }

    public interface IOAuth1Request
    {
        System.Threading.Tasks.Task<IResponse> GetResponseAsync();
        System.Threading.Tasks.Task<IResponse> GetResponseAsync(System.Threading.CancellationToken cancellationToken);
    }

    public interface IOAuth2Request
    {
        System.Threading.Tasks.Task<IResponse> GetResponseAsync();
        System.Threading.Tasks.Task<IResponse> GetResponseAsync(System.Threading.CancellationToken cancellationToken);
        string AccessTokenParameterName { get; set; }
    }

    public interface IResponse // both OAuth1 and OAuth2
    {
        IDictionary<string, string> Headers { get; }
        Uri ResponseUri { get; }
        int StatusCode { get; } // should be System.Net.HttpStatusCode
        //System.IO.Stream GetResponseStream(); // not implemented
        string GetResponseText();
    }

    public class Account // may have to create IAccount interface
    {
        public object xAccount;
        public string Username { get; }
        public Dictionary<string, string> Properties { get; }
        //public CookieContainer Cookies { get; } // not implemented
        public Account( object Account, Dictionary<string, string> Properties, string Username)
        {
            this.xAccount = Account;
            this.Properties = Properties;
            this.Username = Username;
        }
    }

    public interface IAccountStore
    {
        IAccountStore xCreate();
        List<Account> FindAccountsForService(string serviceId);
        void Save(Account account, string serviceId);
        void Delete(Account account, string serviceId);
    }

    public class AccountStore : IAccountStore
    {
        public static IAccountStore xAccountStore;
        public static IAccountStore Create() { return xAccountStore.xCreate(); }
        public IAccountStore xCreate() { return xAccountStore.xCreate(); }
        public List<Account> FindAccountsForService(string serviceId) { return xAccountStore.FindAccountsForService(serviceId); }
        public void Save(Account account, string serviceId) { xAccountStore.Save(account, serviceId); }
        public void Delete(Account account, string serviceId) { xAccountStore.Delete(account, serviceId); }
    }

    public delegate System.Threading.Tasks.Task<string> GetUsernameAsyncFunc(IDictionary<string, string> accountProperties); // TODO: not implemented

    public class AuthenticatorCompletedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get { return Account != null; } }
        public Account Account { get; }
        public AuthenticatorCompletedEventArgs(Account account)
        {
            Account = account;
        }
    }

    public class AuthenticatorErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }
        public AuthenticatorErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = Exception;
        }
    }

    public class OAuth1Request : IOAuth1Request
    {
        private IOAuth1Request request;
        public OAuth1Request(string method, Uri url, Dictionary<string, string> parameters, Account account, bool includeMultiPartInSignature)
        {
            request = Auth.auth.auth1.OAuth1Request(method, url, parameters, account, includeMultiPartInSignature);
        }
        public System.Threading.Tasks.Task<IResponse> GetResponseAsync() { return request.GetResponseAsync(); }
        public System.Threading.Tasks.Task<IResponse> GetResponseAsync(System.Threading.CancellationToken cancellationToken) { return request.GetResponseAsync(cancellationToken); }
    }

    public class OAuth2Request : IOAuth2Request
    {
        IOAuth2Request request;
        public OAuth2Request(string method, Uri url, Dictionary<string, string> parameters, Account account)
        {
            request = Auth.auth.auth2.OAuth2Request(method, url, parameters, account);
        }
        public System.Threading.Tasks.Task<IResponse> GetResponseAsync() { return request.GetResponseAsync(); }
        public System.Threading.Tasks.Task<IResponse> GetResponseAsync(System.Threading.CancellationToken cancellationToken) { return request.GetResponseAsync(cancellationToken); }
        public string AccessTokenParameterName { get { return request.AccessTokenParameterName; } set { request.AccessTokenParameterName = value; } }
    }

    public interface IOAuth1Authenticator
    {
        void OAuth1Authenticator(string consumerKey, string consumerSecret, Uri requestTokenUrl, Uri authorizeUrl, Uri accessTokenUrl, Uri callbackUrl, GetUsernameAsyncFunc getUsernameAsync);
        IOAuth1Request OAuth1Request(string method, Uri url, Dictionary<string, string> parameters, Account account, bool includeMultiPartInSignature);
        event EventHandler<AuthenticatorCompletedEventArgs> Completed;
        event EventHandler<AuthenticatorErrorEventArgs> Error;
    }

    public interface IOAuth2Authenticator
    {
        void OAuth2Authenticator(string clientId, string scope, Uri authorizeUrl, Uri redirectUrl);
        void OAuth2Authenticator(string clientId, string scope, Uri authorizeUrl, Uri redirectUrl, string replacementFormatUrl);
        void OAuth2Authenticator(string clientId, string clientSecret, string scope, Uri authorizeUrl, Uri redirectUrl, Uri accessTokenUrl, GetUsernameAsyncFunc getUsernameAsync);
        IOAuth2Request OAuth2Request(string method, Uri url, Dictionary<string, string> parameters, Account account);
        event EventHandler<AuthenticatorCompletedEventArgs> Completed;
        event EventHandler<AuthenticatorErrorEventArgs> Error;
    }

    public interface IAuth
    {
        IOAuth1Authenticator auth1 { get; set; }
        IOAuth2Authenticator auth2 { get; set; }
    }
}
