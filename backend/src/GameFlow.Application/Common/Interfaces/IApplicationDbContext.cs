using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Common.Interfaces;

/// <summary>
/// Uygulama katmanının veritabanına eriştiği soyutlama. EF Core'un DbContext'i zaten
/// Unit of Work + Repository görevini üstlendiği için ayrıca repository katmanı eklenmez;
/// bu arayüz Application katmanının Infrastructure'a bağımlı olmasını engeller.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<WorkItem> WorkItems { get; }
    DbSet<Label> Labels { get; }
    DbSet<WorkItemLabel> WorkItemLabels { get; }
    DbSet<TaskChecklistItem> TaskChecklistItems { get; }
    DbSet<TaskComment> TaskComments { get; }
    DbSet<TaskAttachment> TaskAttachments { get; }
    DbSet<Sprint> Sprints { get; }
    DbSet<ChatRoom> ChatRooms { get; }
    DbSet<Message> Messages { get; }
    DbSet<MessageAttachment> MessageAttachments { get; }
    DbSet<MessageRead> MessageReads { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Meeting> Meetings { get; }
    DbSet<MeetingAttendee> MeetingAttendees { get; }
    DbSet<CalendarEvent> CalendarEvents { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Proje içindeki görev sayacını atomik olarak artırır ve yeni değeri döner.
    /// Eş zamanlı görev oluşturmalarda aynı numaranın iki kez verilmesini engeller;
    /// bu yüzden okuma-artırma-yazma yerine tek ifadelik bir güncelleme kullanılır.
    /// </summary>
    Task<int> GetNextWorkItemNumberAsync(Guid projectId, CancellationToken cancellationToken = default);
}
