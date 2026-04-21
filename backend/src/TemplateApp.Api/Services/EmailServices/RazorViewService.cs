namespace TemplateApp.Api.Services.EmailServices;

using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class RazorViewService
{
  private readonly IRazorViewEngine _razor;
  private readonly IServiceProvider _serviceProvider;
  private readonly ITempDataProvider _tempDataProvider;

  public RazorViewService(
    IRazorViewEngine razor,
    ITempDataProvider tempDataProvider,
    IServiceProvider serviceProvider
  )
  {
    _razor = razor;
    _tempDataProvider = tempDataProvider;
    _serviceProvider = serviceProvider;
  }

  /// <summary>
  /// Render a Razor View to a string with no model.
  /// </summary>
  /// <param name="viewName">The name/path of the view.</param>
  /// <returns>View.</returns>
  public async Task<(string view, ViewContext context)> RenderToString(string viewName)
    => await RenderToString<object>(viewName);

  /// <summary>
  /// Render a Razor View to a string, using the provided model.
  /// </summary>
  /// <typeparam name="T">Model type.</typeparam>
  /// <param name="viewName">The name/path of the view.</param>
  /// <param name="model">Model to bind to the view.</param>
  /// <param name="culture">Culture to use when rendering the view.</param>
  /// <returns>View.</returns>
  public async Task<(string view, ViewContext context)> RenderToString<T>(
    string viewName,
    T? model = null,
    CultureInfo? culture = null
  ) where T : class
  {
    var actionContext = CreateActionContext(culture);
    var view = FindView(viewName, actionContext);

    await using var output = new StringWriter();

    var viewContext = new ViewContext(
      actionContext,
      view,
      new ViewDataDictionary<T>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
      {
        Model = model!
      },
      new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
      output,
      new HtmlHelperOptions()
    );

    await view.RenderAsync(viewContext);

    return (output.ToString(), viewContext);
  }

  /// <summary>
  ///   <para>
  ///   Tries to find a view taking into account the action context we are in and the culture specified on the request.
  ///   </para>
  ///   <para>
  ///   Starts with most granular culture (e.g. "en-GB") and works back up the hierarchy (e.g. "en") until finally trying no
  ///   culture.
  ///   </para>
  /// </summary>
  /// <param name="viewName">View name/path to find.</param>
  /// <param name="actionContext">Action context for view search.</param>
  /// <returns>View.</returns>
  private IView FindView(string viewName, ActionContext actionContext)
  {
    IEnumerable<string> searchedLocations = new List<string>();

    foreach (var viewPath in GetLocalisedViewPaths(viewName, actionContext))
    {
      var getViewResult = _razor.GetView(null, viewPath, true);
      if (getViewResult.Success)
      {
        return getViewResult.View;
      }

      var findViewResult = _razor.FindView(actionContext, viewPath, true);
      if (findViewResult.Success)
      {
        return findViewResult.View;
      }

      searchedLocations =
        searchedLocations.Concat(getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations));
    }

    var errorMessage = string.Join(
      Environment.NewLine,
      new[]
        {
          $"Unable to find view '{viewName}'. The following locations were searched:"
        }
        .Concat(searchedLocations));

    throw new InvalidOperationException(errorMessage);
  }

  /// <summary>
  /// List all possible view paths based on the current culture within the action context.
  /// </summary>
  /// <param name="viewPath">View path to localise.</param>
  /// <param name="actionContext">>Action context containing culture info.</param>
  /// <returns>List of possible view paths.</returns>
  private static List<string> GetLocalisedViewPaths(string viewPath, ActionContext actionContext)
  {
    var paths = new List<string>();

    var pathSegments = viewPath.Split('/').ToList();

    var culture = actionContext.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture
                  ?? CultureInfo.CurrentUICulture;

    do
    {
      // insert culture into the view path
      // e.g. for
      // Emails/MyEmail
      // return Emails/en-GB/MyEmail
      var segments = new List<string>(pathSegments);
      segments.Insert(segments.Count - 1, culture.Name);

      paths.Add(Path.Combine(segments.ToArray()));

      // Continue up the Culture hierarchy
      culture = culture.Parent;
    } while (!Equals(culture, culture.Parent));

    // Also add the uncultured path as a final fallback
    paths.Add(viewPath);

    return paths;
  }

  /// <summary>
  /// Create an ActionContext for rendering views.
  /// </summary>
  /// <param name="culture">Culture for action context.</param>
  /// <returns>New ActionContext.</returns>
  private ActionContext CreateActionContext(CultureInfo? culture = null)
  {
    var httpContext = new DefaultHttpContext
    {
      RequestServices = _serviceProvider
    };

    if (culture is not null)
    {
      httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(new RequestCulture(culture), null));
    }

    return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
  }
}
