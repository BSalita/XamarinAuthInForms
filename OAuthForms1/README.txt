
Synopsis: Complete C# cross-platform mobile app. Demonstrates requesting of OAuth credentials enabling execution of APIs. Implements Xamarin.Auth in Xamarin.Forms in a natural, PCL-like way.
Uses JSON data file to provide a selection of OAuth logins. Providers include Amazon, Facebook, Google, and many more. 

Features
1. Easy and powerful implementation of Xamarin.Auth in Xamarin.Forms.
2. Complete C# cross-platform mobile app.
3. Use Xamarin.Auth as if it was a native PCL library. No need to push logic into platform dependant files.
4. Demonstrates usage of most Xamarin.Auth members.
5. Compatible with (most) OAuth1 and OAuth2 providers.
6. Uses an easily configurable embedded JSON file to drive execution. File contains OAuth providers, credentials, URLs and much more.
7. Working examples of calling many different OAuth providers.
8. Contains necessary logic to support quirky OAuth implementations.
9. Easily adaptable Xamarin.Auth implementation ready for inclusion in your projects.
10. Sub-classes Xamarin.Auth in PCL for easy use.
11. Designed to be compatible with existing and future versions of Xamarin.Auth.
12. Easy way to test OAuth implementations. Simply enter a new OAuth provider implementation in the JSON file.
13. Demonstrates use of saving/reusing/deleting credentials to minimize repeated authentications.
14. Demonstrates use of refreshing expired authorizations.
15. Demonstrates use of binding to Xamarin.Forms controls.
16. Easier to use than WebView and non-PCL solutions.
17. Complete source code provided. No restrictions on use.

Dependencies.
1. Xamarin.Forms
2. Xamarin.Auth
3. Newtonsoft.Json
4. Visual Studio 2015

Summary of Current Status and Project Intent
This project is intended as a sample of using Xamarin.Auth in Xamarin.Forms. My hopes are that people contribute new providers, enhancements and fixes.
As of this writing, only Android is coded. iPhone and WinPhone are not. The following providers are not fully working; GitHub, LinkedIn, PayPal, StackOverflow.

FAQ
1. How do I add a new provider?
-- Add a new provider object to OAuthProviders.json. To do so, just copy another provider and fill in the details. If your provider is OAuth1, copy an OAuth1 object, same with OAuth2 to OAuth2.
2. I created a new OAuth2 provider object in OAuthProviders.json. Why doesn't it work?
-- Your clientId property may be wrong.
-- Your authorization or token endpoint properties may be wrong.
-- Your redirectUrl property may not conform to the rules of your provider. Some providers require https://. Some require the redirect URL be within the domain specified when you registered at the provider.
-- If all of the above are correct, your provider may have a quirky implementation of OAuth2. See the LinkedIn object as an example of how to fix quirks.
3. Is it flexible enough to handle non-standard or quirky OAuth logins?
-- So far all providers work.
4. Is this project useful to test new authorizations?
-- Yes. See "How do I add a new provider?".
5. How do I test APIs after authorization succeeds?
-- Yes. OAuthProviders.json has a property "apiRequests" which specifieds a list of GET requests to be executed after successful authorization.
6. What Xamarin.Auth members are not fully implemented?
-- See uauthdefs.cs for "ToDo" or "Not implemented". As of this writing, 
7. What providers don't work?
-- The following providers fully authenticate but the GET tests in OAuthProviders.json.apiRequests fail; GitHub, LinkedIn, StackOverflow. Reason unknown.
-- The following providers display an authentication page but reject my login email/password; PayPal (Auth1 and Auth2). Reason unknown.
8. What platforms don't work?
-- As of this writing, src/uauthimpl.cs is working on Android. The file needs the addition of platform specific code to support iPhone and WinPhone, perhaps an additional 12 lines of code for each platform.
  
Project Structure
1. OAuthForms1.sln is the solution file containing the following projects.
2. OAuthForms1.csproj is the sample mobile app. It uses Xamarin.Forms (PCL) and Xamarin.Auth subclassing (PCL). This project contains a default OAuth icon (EmbeddedResoruce). Also contains the all important  JSON data file (EmbeddedResource).
3. UAuth.csproj is a PCL project containing Xamarin.XAuth sub-classing definitions. Composed of one source file.
4. OAuthForms1.(Droid|iOS|WinPhone) are the platform specific projects. Each project is as minimal as possible, containing just one link to the subclassed Xamarin.Auth implementation.
5. src folder contains Xamarin.Forms subclassing. These files are platform independent. They should not be copied into projects, use "Add as Link" instead.
6. Xamarin.Auth - Optional. Either install source from GitHub or include from Nuget.

How to Create your Own Project
1. Add Xamarin.Auth to your own project. Use Nuget (reference/component) or download source from GitHub as was done in OAuthForms1.sln sample.
2. For each Xamarin.Auth referencing project (PCL and all platform projects) add a reference to UAuth. Either add a reference to the dll, or add UAuth.csproj to your solution.
3. Add *a link* (best to not add a copy) of src/uauthimpl.cs to each platform specific project. The source code within uauthimpl.cs will correctly compile as it contains cross-platform code.

If you'd like to contribute to the 
1. Add/test new providers.
2. Implement missing subclassing in oauthdefs.cs and oauthimpl.cs.
3. Debug failing providers (GitHub, LinkedIn, PayPal, StackOverflow).
