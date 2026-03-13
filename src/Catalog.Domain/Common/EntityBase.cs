namespace Catalog.Domain.Common;

/// <summary>
/// Classe de base pour toutes les entités du domaine avec propriétés d'audit.
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
 
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    protected EntityBase()
    {
        Id = Guid.NewGuid();
        CreatedOn = DateTime.UtcNow;
    }

    public void MarkAsDeleted(string deletedBy)
    {
        DeletedOn = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void MarkAsModified(string modifiedBy)
    {
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    public bool IsDeleted { get; set; }
}