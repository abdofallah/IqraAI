**W3CValidator.NET** is a .NET library to perform validation of CSS/(X)HTML documents, using [WWW Consortium's](http://www.w3.org) web services.

**NuGet package** : https://www.nuget.org/packages/W3CValidator

It utilizes the following W3C services :

1. [Markup Validation Service](http://validator.w3.org/docs/api.html)

2. [CSS Validation Service](http://jigsaw.w3.org/css-validator/api.html)

***

**Support**

This project needs your support for further developments ! Please consider donating.

- _Yandex.Money_ : 41001577953208

- _WebMoney (WMR)_ : R399275865890

[![Image](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=APHM8MU9N76V8 "Donate")

***

**W3C CSS Validation Service**

`ICssValidationResult result = Validator.Validate.Css().Document("body { color : white }");`

`ICssValidationResult result = Validator.Validate.Css().Url("http://www.w3.org/2008/site/css/minimum", request => request.Language("ru").Medium(CssMedium.All).Profile(CssProfile.Css2).Warnings(WarningsLevel.Important));`

**W3C Markup Validation Service**

`IMarkupValidationResult result = Validator.Validate.Markup().Url("http://wwww.w3.org");`

`IMarkupValidationResult result = Validator.Validate.Markup().Url("http://wwww.w3.org", request => request.Encoding(Encoding.UTF32));`