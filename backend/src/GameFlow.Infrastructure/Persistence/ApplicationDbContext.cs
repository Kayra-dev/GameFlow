using System.Linq.Expressions;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Domain.Common;
using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL üzerinde çalışan uygulama veritabanı bağlamı (Code First).
/// Varlık yapılandırmaları <see cref="Configurations"/> altındaki
/// IEntityTypeConfiguration sınıflarında tutulur.
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService? currentUserService = null)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<WorkItemLabel> WorkItemLabels => Set<WorkItemLabel>();
    public DbSet<TaskChecklistItem> TaskChecklistItems => Set<TaskChecklistItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<MessageRead> MessageReads => Set<MessageRead>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingAttendee> MeetingAttendees => Set<MeetingAttendee>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    /// <summary>
    /// Veritabanında saklanan dosya içerikleri. Yalnızca
    /// <c>FileStorage:Provider = Database</c> iken doldurulur.
    /// </summary>
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    /// <inheritdoc />
    public async Task<int> GetNextWorkItemNumberAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        // UPDATE ... RETURNING tek atomik ifade olduğu için satır kilidini veritabanı
        // yönetir; ayrı bir işlem (transaction) veya iyimser yeniden deneme gerekmez.
        const string sql =
            "UPDATE \"Projects\" SET \"WorkItemCounter\" = \"WorkItemCounter\" + 1 " +
            "WHERE \"Id\" = {0} RETURNING \"WorkItemCounter\" AS \"Value\"";

        var numbers = await Database
            .SqlQueryRaw<int>(sql, projectId)
            .ToListAsync(cancellationToken);

        return numbers.Count > 0
            ? numbers[0]
            : throw new InvalidOperationException(
                $"Görev numarası üretilemedi; proje bulunamadı: {projectId}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplySoftDeleteQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // PostgreSQL "timestamp with time zone" yalnızca UTC DateTime kabul eder.
        // Tüm DateTime alanları okuma/yazmada UTC'ye normalize edilir.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();

        // Varsayılan olarak sınırsız metin yerine makul uzunluklar kullanılır;
        // istisnalar ilgili konfigürasyon sınıflarında ezilir.
        configurationBuilder.Properties<string>().HaveMaxLength(512);

        base.ConfigureConventions(configurationBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    /// <summary>
    /// CreatedAt/UpdatedAt alanlarını otomatik doldurur ve silme isteklerini
    /// mantıksal silmeye çevirir.
    /// </summary>
    private void ApplyAuditInformation()
    {
        var now = DateTime.UtcNow;
        var actorId = currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = entry.Entity.CreatedAt == default ? now : entry.Entity.CreatedAt;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedById = actorId;
        }
    }

    /// <summary>
    /// <see cref="ISoftDeletable"/> uygulayan tüm varlıklara "IsDeleted == false"
    /// global filtresini ekler.
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(property), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
