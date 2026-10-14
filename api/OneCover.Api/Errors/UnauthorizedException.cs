namespace OneCover.Api.Errors;

/// <summary>
/// Exception thrown when the user cannot be verified e.g. wrong username and / or password, returned as 401
/// </summary>
public class UnauthorizedException(string message) : Exception(message);