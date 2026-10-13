namespace OneCover.Api.Errors;

/// <summary>
/// Exception is thrown when a request breaks a business rule like an invalid claim status move. 
/// </summary>
public class BusinessRuleException(string message) : Exception(message);