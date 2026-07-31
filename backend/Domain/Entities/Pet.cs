using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public sealed class Pet : TenantOwnedEntity
{
    private const int MaxNameLength = 120;
    private const int MaxBreedLength = 120;
    private const int MaxNotesLength = 1000;
    private const int MaxPhotoUrlLength = 500;

    private Pet()
    {
        // Necessário para o EF Core.
    }

    public Pet(
        Guid tenantId,
        Guid companyId,
        Guid customerProfileId,
        string name,
        PetSpecies species,
        PetSize size,
        string? breed = null,
        decimal? weightKg = null,
        DateOnly? birthDate = null)
        : base(tenantId)
    {
        ValidateRequiredId(companyId, nameof(companyId));
        ValidateRequiredId(customerProfileId, nameof(customerProfileId));

        CompanyId = companyId;
        CustomerProfileId = customerProfileId;
        Name = NormalizeRequiredText(name, MaxNameLength, nameof(name));
        Species = species;
        Size = size;
        Breed = NormalizeOptionalText(breed, MaxBreedLength, nameof(breed));

        SetWeight(weightKg);
        SetBirthDate(birthDate);
    }

    public Guid CompanyId { get; private set; }

    public Guid CustomerProfileId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public PetSpecies Species { get; private set; }

    public PetSize Size { get; private set; }

    public string? Breed { get; private set; }

    public decimal? WeightKg { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public string? BehaviorNotes { get; private set; }

    public string? AllergyNotes { get; private set; }

    public string? Restrictions { get; private set; }

    public string? PhotoUrl { get; private set; }

    public void UpdateBasicInformation(
        string name,
        PetSpecies species,
        PetSize size,
        string? breed,
        decimal? weightKg,
        DateOnly? birthDate)
    {
        Name = NormalizeRequiredText(name, MaxNameLength, nameof(name));
        Species = species;
        Size = size;
        Breed = NormalizeOptionalText(breed, MaxBreedLength, nameof(breed));

        SetWeight(weightKg);
        SetBirthDate(birthDate);

        Touch();
    }

    public void UpdateNotes(
        string? behaviorNotes,
        string? allergyNotes,
        string? restrictions)
    {
        BehaviorNotes = NormalizeOptionalText(
            behaviorNotes,
            MaxNotesLength,
            nameof(behaviorNotes));

        AllergyNotes = NormalizeOptionalText(
            allergyNotes,
            MaxNotesLength,
            nameof(allergyNotes));

        Restrictions = NormalizeOptionalText(
            restrictions,
            MaxNotesLength,
            nameof(restrictions));

        Touch();
    }

    public void SetPhoto(string? photoUrl)
    {
        PhotoUrl = NormalizeOptionalText(
            photoUrl,
            MaxPhotoUrlLength,
            nameof(photoUrl));

        Touch();
    }

    private void SetWeight(decimal? weightKg)
    {
        if (weightKg is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weightKg),
                "O peso deve ser maior que zero.");
        }

        WeightKg = weightKg;
    }

    private void SetBirthDate(DateOnly? birthDate)
    {
        if (birthDate.HasValue &&
            birthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentOutOfRangeException(
                nameof(birthDate),
                "A data de nascimento não pode estar no futuro.");
        }

        BirthDate = birthDate;
    }

    private static void ValidateRequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador é obrigatório.",
                parameterName);
        }
    }

    private static string NormalizeRequiredText(
        string value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "O valor é obrigatório.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"O valor deve possuir no máximo {maxLength} caracteres.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"O valor deve possuir no máximo {maxLength} caracteres.",
                parameterName);
        }

        return normalized;
    }
}