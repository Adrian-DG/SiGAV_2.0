using Application.Contracts;
using Domain.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistance.Interceptors;

/// <summary>
/// Completa los campos de auditoría de toda entidad <see cref="IAuditableMetadata"/>
/// (en SiGAV 1.0 se asignaban a mano en cada repositorio, con UsuarioId = 1).
/// </summary>
public class AuditableEntityInterceptor(ICurrentUserService currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        // Los registros de auditoría solo pueden insertarse
        var alterado = context.ChangeTracker.Entries<IRegistroInmutable>()
            .FirstOrDefault(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (alterado is not null)
            throw new InvalidOperationException(
                $"'{alterado.Metadata.ClrType.Name}' es un registro de auditoría inmutable: no puede modificarse ni eliminarse.");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var userId = currentUser.UserId ?? 0;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableMetadata>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = today;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = today;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }
}
