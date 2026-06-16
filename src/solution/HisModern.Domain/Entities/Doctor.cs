namespace HisModern.Domain.Entities;

/// <summary>醫師 (醫師 / Doctor)。取代 legacy 的 <c>Yishi</c>。</summary>
public sealed class Doctor
{
    public Doctor(int id, string name, string departmentCode)
    {
        Id = id;
        Name = name;
        DepartmentCode = departmentCode;
    }

    public int Id { get; }
    public string Name { get; }
    public string DepartmentCode { get; }
}
