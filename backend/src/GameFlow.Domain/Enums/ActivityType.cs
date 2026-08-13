namespace GameFlow.Domain.Enums;

/// <summary>ActivityLogs tablosunda tutulan denetim kaydı türleri.</summary>
public enum ActivityType
{
    UserLoggedIn = 1,
    UserCreated = 2,
    UserUpdated = 3,
    UserDeleted = 4,
    TeamCreated = 5,
    TeamUpdated = 6,
    TeamDeleted = 7,
    TeamMemberAdded = 8,
    TeamMemberRemoved = 9,
    ProjectCreated = 10,
    ProjectUpdated = 11,
    ProjectDeleted = 12,
    TaskCreated = 13,
    TaskUpdated = 14,
    TaskStatusChanged = 15,
    TaskAssigned = 16,
    TaskDeleted = 17,
    TaskCommented = 18,
    AttachmentUploaded = 19,
    AttachmentDeleted = 20,
    SprintCreated = 21,
    SprintStarted = 22,
    SprintCompleted = 23,
    MeetingCreated = 24,
    AnnouncementPublished = 25
}
