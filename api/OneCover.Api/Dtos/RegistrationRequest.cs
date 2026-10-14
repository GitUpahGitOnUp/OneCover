using System.ComponentModel.DataAnnotations;

namespace OneCover.Api.Dtos;

/// <summary>
/// New member sign-up. There's purposefully no Role specificity. Anyone who signs up is a Member. 
/// </summary>
public record RegistrationRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string LastName  { get; init; } = string.Empty;

    [Required]
    public DateOnly? HireDate {get; init; } 

    [Required, Range(0, 10_000_000)]
    public decimal? AnnualSalary { get; init;} 

    [Required, Range(0, 8760)] // 8760 = hours in a year
    public int? HoursWorkedLast12Months { get; init; } 

    [Required, RegularExpression(@"^\d{4}$")] // exactly 4 digits
    public string SsnLast4 { get; init; } = string.Empty;

}