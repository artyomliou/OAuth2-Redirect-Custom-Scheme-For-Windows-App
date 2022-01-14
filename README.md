OAuth2 Redirect w/ Custom Scheme For Windows App
================================================

Demostrating how to login by **external browser** through **OAuth2 auhtorization flow with PKCE**,
then redirect to **Custom Scheme**(ex: ```myapp://redirect/```) registered by windows app.

This project is based on:
- [OAuth for Apps: Samples for Windows](https://github.com/googlesamples/oauth-apps-for-windows)
- [Simon Mourier's self-sufficient, 3rd party-free, WPF sample](https://stackoverflow.com/questions/48321034/wpf-application-authentication-with-google/48457204#48457204)


In case there's no "revoke endpoint" from your OAuth provider:
I combined these code, modified for integrating with AWS Cognito user pool.
So, remove these part as you wish.


# Reference
- [Simon Mourier's self-sufficient, 3rd party-free, WPF sample](https://stackoverflow.com/questions/48321034/wpf-application-authentication-with-google/48457204#48457204)
- [LGM-AdrianHum/ApplicationStartup.cs](https://gist.github.com/LGM-AdrianHum/d16a6b49d1b7644b2b9f88f85db2d41e)
- Show output on another thread [link](https://stackoverflow.com/questions/11625208/accessing-ui-main-thread-safely-in-wpf/34554362#34554362)
- [Registering an Application to a URI Scheme using .NET (custom protocol)](https://www.meziantou.net/registering-an-application-to-a-uri-scheme-using-net.htm)

# TODO
- Review license