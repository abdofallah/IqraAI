using Catharsis.Extensions;
using Catharsis.Fixture;
using FluentAssertions;
using Newtonsoft.Json;
using W3CValidator.Css;
using W3CValidator.Markup;

namespace W3CValidator.Tests;

public class Test : IDisposable
{
  protected Test()
  {
    JsonConvert.DefaultSettings = () => new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

    Fixture.Current.Configuration.Type<CancellationToken>(x => x.Constructor(() => new CancellationToken(true)));
    
    Fixture.Current.Configuration
      .Map<IError, Error>()
      .Map<W3CValidator.Css.IErrorsGroup, W3CValidator.Css.ErrorsGroup>()
      .Map<IWarning, Warning>()
      .Map<W3CValidator.Css.IWarningsGroup, W3CValidator.Css.WarningsGroup>()
      .Map<IIssues, Issues>()
      .Map<ICssValidationRequest, CssValidationRequest>()
      .Map<ICssValidationResult, CssValidationResult>()

      .Map<W3CValidator.Markup.IErrorsGroup, W3CValidator.Markup.ErrorsGroup>()
      .Map<W3CValidator.Markup.IWarningsGroup, W3CValidator.Markup.WarningsGroup>()
      .Map<IIssue, Issue>()
      .Map<IMarkupValidationRequest, MarkupValidationRequest>()
      .Map<IMarkupValidationResult, MarkupValidationResult>();
  }

  public virtual void Dispose()
  {
  }

  protected void TestCompareTo<TClass, TProperty>(string property, TProperty lower, TProperty greater, Func<TClass> constructor = null)
  {
    constructor ??= () => typeof(TClass).Instance<TClass>();

    var first = constructor().To<IComparable<TClass>>();
    var second = constructor().To<TClass>();

    first.SetPropertyValue(property, lower);
    second.SetPropertyValue(property, lower);

    first.CompareTo(second).Should().Be(0);
    second.SetPropertyValue(property, greater);
    first.CompareTo(second).Should().BeLessThan(0);
  }

  protected void TestEquality<TClass, TProperty>(string property, TProperty oldValue, TProperty newValue, Func<TClass> constructor = null)
  {
    constructor ??= () => typeof(TClass).Instance<TClass>();
    var entity = constructor();

    entity.Equals(new object()).Should().BeFalse();
    entity.Equals(null).Should().BeFalse();
    entity.Equals(entity).Should().BeTrue();
    //entity.Equals(constructor()).Should().BeTrue();

    constructor().SetPropertyValue(property, oldValue).Equals(constructor().SetPropertyValue(property, oldValue)).Should().BeTrue();
    constructor().SetPropertyValue(property, oldValue).Equals(constructor().SetPropertyValue(property, newValue)).Should().BeFalse();
  }

  protected void TestHashCode<TClass, TProperty>(string property, TProperty oldValue, TProperty newValue, Func<TClass> constructor = null)
  {
    constructor ??= () => typeof(TClass).Instance<TClass>();
    var entity = constructor();

    entity.GetHashCode().Should().Be(entity.GetHashCode());
    //entity.GetHashCode().Should().Be(constructor().GetHashCode());

    constructor().SetPropertyValue(property, oldValue).GetHashCode().Should().Be(constructor().SetPropertyValue(property, oldValue).GetHashCode());
    constructor().SetPropertyValue(property, oldValue).GetHashCode().Should().NotBe(constructor().SetPropertyValue(property, newValue).GetHashCode());
  }
}