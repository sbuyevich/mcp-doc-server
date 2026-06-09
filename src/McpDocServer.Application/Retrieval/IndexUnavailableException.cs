namespace McpDocServer.Application.Retrieval;

public sealed class IndexUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
