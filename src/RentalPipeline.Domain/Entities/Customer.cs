namespace RentalPipeline.Domain.Entities;

/// <summary>
/// Represents a customer who can submit rental proposals.
/// </summary>
public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Customer()
    {
        // Required by EF Core.
    }

    public Customer(string name, string email, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        Phone = phone;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the customer's editable details. Not part of the original challenge
    /// specification (which only documents Create/Get for customers) — added on explicit
    /// request to provide full CRUD.
    /// </summary>
    public void UpdateDetails(string name, string email, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Name = name;
        Email = email;
        Phone = phone;
    }
}
