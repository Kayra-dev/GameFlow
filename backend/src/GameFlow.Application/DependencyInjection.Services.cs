using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Common.Services;
using GameFlow.Application.Features.Auth;
using GameFlow.Application.Features.Announcements;
using GameFlow.Application.Features.Calendar;
using GameFlow.Application.Features.Chat;
using GameFlow.Application.Features.Dashboard;
using GameFlow.Application.Features.Notifications;
using GameFlow.Application.Features.Reports;
using GameFlow.Application.Features.Search;
using GameFlow.Application.Features.Projects;
using GameFlow.Application.Features.Sprints;
using GameFlow.Application.Features.WorkItems;
using GameFlow.Application.Features.Teams;
using GameFlow.Application.Features.Users;
using Microsoft.Extensions.DependencyInjection;

namespace GameFlow.Application;

/// <summary>
/// Modül servislerinin kaydı. Yeni bir modül eklendiğinde yalnızca burası güncellenir.
/// </summary>
public static class ApplicationServiceRegistration
{
    internal static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IProjectService, ProjectService>();

        services.AddScoped<IWorkItemService, WorkItemService>();
        services.AddScoped<IWorkItemChecklistService, WorkItemChecklistService>();
        services.AddScoped<IWorkItemCommentService, WorkItemCommentService>();
        services.AddScoped<IWorkItemAttachmentService, WorkItemAttachmentService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<ISprintService, SprintService>();

        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISearchService, SearchService>();

        return services;
    }
}
