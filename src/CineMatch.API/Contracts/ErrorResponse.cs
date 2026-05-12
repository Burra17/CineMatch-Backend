namespace CineMatch.API.Contracts;

// Class to return a errorresponse with details to the user/developer
public class ErrorResponse
{
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string? Detail { get; set; }
}