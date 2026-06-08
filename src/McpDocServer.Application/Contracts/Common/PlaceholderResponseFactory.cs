namespace McpDocServer.Application.Contracts.Common;

/// <summary>
/// Creates consistent Stage 1 placeholder responses.
/// </summary>
public static class PlaceholderResponseFactory
{
    /// <summary>
    /// Stable placeholder error code.
    /// </summary>
    public const string ErrorCode = "stage_not_implemented";

    /// <summary>
    /// Stable placeholder error message.
    /// </summary>
    public const string ErrorMessage = "This capability is planned for a later stage.";

    /// <summary>
    /// Creates a not-ready response for a concrete tool response type.
    /// </summary>
    public static TResponse Create<TResponse, TData>()
        where TResponse : ToolResponse<TData>, new()
    {
        return new TResponse
        {
            Status = ToolResultStatus.NotReady,
            Errors =
            [
                new ToolError
                {
                    Code = ErrorCode,
                    Message = ErrorMessage
                }
            ]
        };
    }
}
