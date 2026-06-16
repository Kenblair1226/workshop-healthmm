namespace HisModern.Domain.Entities;

/// <summary>科別 (科別 / Department)。取代 legacy 的 <c>Kebie</c>。</summary>
public sealed class Department
{
    public Department(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; }
    public string Name { get; }
}
