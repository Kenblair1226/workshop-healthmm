using HisModern.Domain.Enums;
using HisModern.Domain.ValueObjects;

namespace HisModern.Domain.Entities;

/// <summary>
/// 病患 (病患 / Patient)。取代 legacy 的 <c>Binghuan</c> public field 結構。
/// </summary>
public sealed class Patient
{
    /// <summary>身分證字號的有效長度。</summary>
    public const int IdNumberLength = 10;

    public Patient(int id, string name, Gender gender, BirthDate birthDate, string idNumber, string? phone)
    {
        Id = id;
        Name = name;
        Gender = gender;
        BirthDate = birthDate;
        IdNumber = idNumber;
        Phone = phone;
    }

    public int Id { get; }
    public string Name { get; }
    public Gender Gender { get; }
    public BirthDate BirthDate { get; }
    public string IdNumber { get; }
    public string? Phone { get; }

    /// <summary>身分證字號是否符合長度規則 (取代 legacy 重複的 idno.Length != 10 檢核)。</summary>
    public bool HasValidIdNumber => IdNumber is { Length: IdNumberLength };

    /// <summary>到指定日期為止的足歲年齡。</summary>
    public int AgeAsOf(DateOnly asOf) => BirthDate.AgeAsOf(asOf);
}
