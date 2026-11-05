using System.Net.WebSockets;
using LanguageExt;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;

namespace Tests.Common;

public static class TestsExtensions
{
    public static T RightOrThrow<TE, T>(this Either<TE, T> result)
        where TE : Exception
    {
        return result.Match<T>(
            r => r,
            e => throw new ArgumentException("Expected Right but got Left", e));
    }

    public static TE LeftOrThrow<TE, T>(this Either<TE, T> result)
        where TE : Exception
    {
        return result.Match<TE>(
            _ => throw new ArgumentException("Expected Left but got Right"),
            e => e);
    }

    public static async Task<T> ToResponseModel<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<T>(content)
               ?? throw new ArgumentException("Response content cannot be null.");
    }

    public static HubConnection GetHubConnection(this TestServer testServer, string hubUrl)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(testServer.BaseAddress, hubUrl), options =>
            {
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
                options.WebSocketFactory = (context, cancellationToken) =>
                {
                    var webSocketClient = testServer.CreateWebSocketClient();
                    var webSocketTask = webSocketClient.ConnectAsync(context.Uri, cancellationToken);
                    return new ValueTask<WebSocket>(webSocketTask);
                };
            })
            .Build();
    }
}