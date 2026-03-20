using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Errors;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Domain.Entities;

/// <summary>
/// Domain behavior methods for LiveSession entity
/// Rich Domain Model following Clean Architecture principles
/// </summary>
public partial class LiveSession
{
    // Business rules constants
    private const int MinSessionNameLength = 3;
    private const int MaxSessionNameLength = 100;
    private const int MaxDescriptionLength = 500;
    private const int MinMaxListeners = 1;
    private const int MaxMaxListeners = 10000;

    /// <summary>
    /// Factory method to create a new live session
    /// 
    /// BUSINESS RULES:
    /// - Session name must be 3-100 characters
    /// - Host user ID cannot be empty
    /// - Max listeners must be between 1-10000
    /// - Session starts in Created or Scheduled state
    /// </summary>
    /// <exception cref="LiveSessionValidationException">When validation fails</exception>
    public static LiveSession Create(
        Guid hostUserId,
        string sessionName,
        string? description,
        Guid? azuraCastStationId,
        int maxListeners,
        bool isPublic,
        string? genre,
        DateTime? scheduledStartAt,
        IDateTimeProvider dateTimeProvider)
    {
        // Validate host user ID
        if (hostUserId == Guid.Empty)
            throw new LiveSessionValidationException(
                "Host user ID cannot be empty",
                LiveSessionErrorCodes.HostUserIdEmpty);

        // Validate session name
        if (string.IsNullOrWhiteSpace(sessionName))
            throw new LiveSessionValidationException(
                "Session name cannot be empty",
                LiveSessionErrorCodes.SessionNameEmpty);

        var trimmedName = sessionName.Trim();
        if (trimmedName.Length < MinSessionNameLength)
            throw new LiveSessionValidationException(
                $"Session name must be at least {MinSessionNameLength} characters",
                LiveSessionErrorCodes.SessionNameTooShort);

        if (trimmedName.Length > MaxSessionNameLength)
            throw new LiveSessionValidationException(
                $"Session name cannot exceed {MaxSessionNameLength} characters",
                LiveSessionErrorCodes.SessionNameTooLong);

        // Validate description
        if (description != null && description.Length > MaxDescriptionLength)
            throw new LiveSessionValidationException(
                $"Description cannot exceed {MaxDescriptionLength} characters",
                LiveSessionErrorCodes.DescriptionTooLong);

        // Validate max listeners
        if (maxListeners < MinMaxListeners || maxListeners > MaxMaxListeners)
            throw new LiveSessionValidationException(
                $"Max listeners must be between {MinMaxListeners} and {MaxMaxListeners}",
                LiveSessionErrorCodes.InvalidMaxListeners);

        var now = dateTimeProvider.UtcNow;
        var isScheduled = scheduledStartAt.HasValue && scheduledStartAt.Value > now;

        return new LiveSession
        {
            Id = Guid.NewGuid(),
            HostUserId = hostUserId,
            AzuraCastStationId = azuraCastStationId,
            SessionName = trimmedName,
            Description = description?.Trim(),
            Status = isScheduled ? SessionStatus.Scheduled : SessionStatus.Created,
            StartedAt = isScheduled ? scheduledStartAt!.Value : now,
            CreatedAt = now,
            MaxListeners = maxListeners,
            IsPublic = isPublic,
            Genre = genre?.Trim()
        };
    }

    public void Schedule(DateTime startTime, IDateTimeProvider dateTimeProvider)
    {
        if (startTime <= dateTimeProvider.UtcNow)
            throw new LiveSessionValidationException(
                "Scheduled start time must be in the future",
                LiveSessionErrorCodes.SessionNotActive);

        if (Status == SessionStatus.Live || Status == SessionStatus.Paused)
            throw new InvalidSessionStateException(
                "Cannot schedule a session that is already running",
                LiveSessionErrorCodes.SessionAlreadyActive);

        if (Status == SessionStatus.Ended)
            throw new InvalidSessionStateException(
                "Cannot schedule an ended session",
                LiveSessionErrorCodes.SessionAlreadyEnded);

        if (Status == SessionStatus.Cancelled)
            throw new InvalidSessionStateException(
                "Cannot schedule a cancelled session",
                LiveSessionErrorCodes.SessionNotActive);

        Status = SessionStatus.Scheduled;
        StartedAt = startTime;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Starts the live session.
    /// </summary>
    /// <exception cref="InvalidSessionStateException">When session is already active</exception>
    public void Start(IDateTimeProvider dateTimeProvider)
    {
        if (Status == SessionStatus.Live)
            throw new InvalidSessionStateException(
                "Session is already active",
                LiveSessionErrorCodes.SessionAlreadyActive);

        if (Status == SessionStatus.Ended)
            throw new InvalidSessionStateException(
                "Cannot restart an ended session",
                LiveSessionErrorCodes.SessionAlreadyEnded);

        if (Status == SessionStatus.Cancelled)
            throw new InvalidSessionStateException(
                "Cannot start a cancelled session",
                LiveSessionErrorCodes.SessionNotActive);

        Status = SessionStatus.Live;
        StartedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Pause an active live session.
    /// </summary>
    public void Pause(IDateTimeProvider dateTimeProvider)
    {
        if (Status != SessionStatus.Live)
            throw new InvalidSessionStateException(
                "Only active sessions can be paused",
                LiveSessionErrorCodes.SessionNotActive);

        Status = SessionStatus.Paused;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Resume a paused live session.
    /// </summary>
    public void Resume(IDateTimeProvider dateTimeProvider)
    {
        if (Status != SessionStatus.Paused)
            throw new InvalidSessionStateException(
                "Only paused sessions can be resumed",
                LiveSessionErrorCodes.SessionNotActive);

        Status = SessionStatus.Live;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Stops/Ends the live session.
    /// </summary>
    public void Stop(IDateTimeProvider dateTimeProvider)
    {
        End(dateTimeProvider);
    }

    /// <summary>
    /// Ends the live session
    /// 
    /// BUSINESS RULES:
    /// - Session must be in Active state
    /// - Cannot end a session that's already ended
    /// </summary>
    /// <exception cref="InvalidSessionStateException">When session is not active</exception>

    public void End(IDateTimeProvider dateTimeProvider)
    {
        if (Status == SessionStatus.Ended)
            throw new InvalidSessionStateException(
                "Session is already ended",
                LiveSessionErrorCodes.SessionAlreadyEnded);

        if (Status != SessionStatus.Live && Status != SessionStatus.Paused)
            throw new InvalidSessionStateException(
                "Only active or paused sessions can be ended",
                LiveSessionErrorCodes.SessionNotActive);

        Status = SessionStatus.Ended;
        EndedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates session details
    /// </summary>
    public void UpdateDetails(
        string sessionName,
        string? description,
        string? genre,
        IDateTimeProvider dateTimeProvider)
    {
        if (Status != SessionStatus.Live)
            throw new InvalidSessionStateException(
                "Cannot update details of non-active session",
                LiveSessionErrorCodes.SessionNotActive);

        // Validate session name
        if (string.IsNullOrWhiteSpace(sessionName))
            throw new LiveSessionValidationException(
                "Session name cannot be empty",
                LiveSessionErrorCodes.SessionNameEmpty);

        var trimmedName = sessionName.Trim();
        if (trimmedName.Length < MinSessionNameLength)
            throw new LiveSessionValidationException(
                $"Session name must be at least {MinSessionNameLength} characters",
                LiveSessionErrorCodes.SessionNameTooShort);

        if (trimmedName.Length > MaxSessionNameLength)
            throw new LiveSessionValidationException(
                $"Session name cannot exceed {MaxSessionNameLength} characters",
                LiveSessionErrorCodes.SessionNameTooLong);

        // Validate description
        if (description != null && description.Length > MaxDescriptionLength)
            throw new LiveSessionValidationException(
                $"Description cannot exceed {MaxDescriptionLength} characters",
                LiveSessionErrorCodes.DescriptionTooLong);

        SessionName = trimmedName;
        Description = description?.Trim();
        Genre = genre?.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Checks if session is active
    /// </summary>
    public bool IsActive() => Status == SessionStatus.Live;

    /// <summary>
    /// Checks if user is the host
    /// </summary>
    public bool IsHost(Guid userId) => HostUserId == userId;

    /// <summary>
    /// Verifies user is authorized to perform host actions
    /// </summary>
    public void EnsureIsHost(Guid userId)
    {
        if (!IsHost(userId))
            throw new SessionAuthorizationException(
                "Only the session host can perform this action",
                LiveSessionErrorCodes.NotSessionHost);
    }
}
