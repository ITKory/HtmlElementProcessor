using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using HtmlElementProcessor.Models;
using HtmlElementProcessor.Services;
using HtmlElementProcessor.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
	foreach (var filter in options.Filters.OfType<UnsupportedContentTypeFilter>().ToArray())
		options.Filters.Remove(filter);
}).AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions))
.ConfigureApiBehaviorOptions(options =>
{
	options.SuppressMapClientErrors = true;
	options.InvalidModelStateResponseFactory = context =>
	{
		var unsupportedContentType = context.ModelState.Values.SelectMany(state => state.Errors)
			.Any(error => error.Exception is UnsupportedContentTypeException);
		return new ObjectResult(new ProcessElementsResponse
		{
			IsError = 1,
			ErrorCode = ErrorCodes.ValidationError,
			ErrorMessage = unsupportedContentType
				? "Content-Type must be application/json."
				: "Request body must be a JSON object with string fields."
		})
		{
			StatusCode = unsupportedContentType
				? StatusCodes.Status415UnsupportedMediaType
				: StatusCodes.Status400BadRequest
		};
	};
});

builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));
builder.Services.AddScoped<IValidator<ProcessElementsRequest>, ProcessElementsRequestValidator>();
builder.Services.AddScoped<ElementProcessingService>();

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(
	builder.Configuration.GetConnectionString("Postgres")
	?? throw new InvalidOperationException("Connection string 'Postgres' is required.")));

builder.Services.AddScoped<PostgresElementStore>();
builder.Services.AddScoped<IElementStore>(provider => provider.GetRequiredService<PostgresElementStore>());

builder.Services.AddOpenApi(options => options.AddSchemaTransformer((schema, context, _) =>
{
	if (context.JsonTypeInfo.Type != typeof(ProcessElementsRequest)) return Task.CompletedTask;
	schema.Required = schema.Properties!.Keys.ToHashSet();
	foreach (var property in schema.Properties.Values.OfType<OpenApiSchema>())
		property.Type = JsonSchemaType.String;
	return Task.CompletedTask;
}));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
	await scope.ServiceProvider.GetRequiredService<PostgresElementStore>()
		.InitializeAsync(app.Lifetime.ApplicationStopping);
}

app.MapOpenApi("/api/swagger/{documentName}.json");
app.UseSwaggerUI(options =>
{
	options.RoutePrefix = "api/swagger";
	options.SwaggerEndpoint("/api/swagger/v1.json", "HtmlElementProcessor API v1");
});

app.MapControllers();

await app.RunAsync();

static void ConfigureJson(JsonSerializerOptions options)
{
	options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
	options.WriteIndented = true;
	options.NumberHandling = JsonNumberHandling.Strict;
}
