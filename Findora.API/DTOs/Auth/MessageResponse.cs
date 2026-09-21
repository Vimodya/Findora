namespace Findora.API.DTOs.Auth;

/// <summary>Generic acknowledgement shape for endpoints with no other payload to return.</summary>
public class MessageResponse
{
    public string Message { get; set; } = string.Empty;

    public MessageResponse() { }

    public MessageResponse(string message) => Message = message;
}
