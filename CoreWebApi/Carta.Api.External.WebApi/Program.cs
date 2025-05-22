using Carta.Api.External.Logic.Processor;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// TODO: configure services and logging as needed

var app = builder.Build();

app.MapPost("/CartaExternalAPI", ([FromBody] string body) =>
{
    var processor = new ExternalApiProcessor(body);
    if (!processor.TryProcessPostRequest(Guid.NewGuid().ToString("N"), out var response))
    {
        return Results.BadRequest();
    }

    return Results.Ok(response);
});

app.MapPost("/CartaAPI", ([FromBody] string body) =>
{
    var processor = new GtwApiProcessor(body);
    var result = processor.TryProcessPostRequest(out var response, out var statusCode);
    return Results.StatusCode((int)statusCode, response);
});

app.MapPost("/GetClientId", ([FromBody] string body) =>
{
    var processor = new GtwApiProcessor(body, false);
    processor.TryProcessGetClientRequest(out var response, out var statusCode);
    return Results.StatusCode((int)statusCode, response);
});

// TODO: migrate remaining endpoints from IService to Web API

app.Run();
