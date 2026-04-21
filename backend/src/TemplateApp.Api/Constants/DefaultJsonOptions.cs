namespace TemplateApp.Api.Constants;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

public static class DefaultJsonOptions
{
  public static readonly Action<JsonOptions> Configure = o =>
  {
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
  };

  public static JsonSerializerOptions Serializer
  {
    get
    {
      var o = new JsonOptions();
      Configure(o);
      return o.JsonSerializerOptions;
    }
  }
}
