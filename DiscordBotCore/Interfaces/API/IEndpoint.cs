using System.Threading.Tasks;
using DiscordBotCore.API;
using DiscordBotCore.Others;

namespace DiscordBotCore.Interfaces.API;

public enum EndpointType
{
    Get,
    Post,
    Put,
    Delete
}

public interface IEndpoint
{
    public string Path { get; }
    public EndpointType HttpMethod { get; }
    public Task<ApiResponse> HandleRequest(string? jsonRequest);
}
