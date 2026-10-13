namespace OneCover.Api.Errors;

/// <summary>Thrown when a requested record doesn't exist or doesn't belong to the current user. Returned as 404.</summary>
public class NotFoundException(string message) : Exception(message); // no class body means class dec. ends in ';' vs '{}'. Acceptable since C# v.12

// : Exception(message) inherits from .NET's built-in Exception and passes a message up to it. 

// why make the class type at all? The type now carries implicit meaning, the handler can tell "not found" from
// "a rule was broken" w/o reading the message text.