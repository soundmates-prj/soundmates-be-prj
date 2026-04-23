using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionChats;

public sealed record GetLiveSessionChatsQuery(Guid SessionId) : IQuery<List<LiveSessionChatResult>>;

public sealed class GetLiveSessionChatsHandler : IQueryHandler<GetLiveSessionChatsQuery, List<LiveSessionChatResult>>
{
    private readonly ILiveSessionRepository _repository;

    public GetLiveSessionChatsHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<LiveSessionChatResult>>> Handle(GetLiveSessionChatsQuery request, CancellationToken cancellationToken)
    {
        var chats = await _repository.GetSessionChatsAsync(request.SessionId, cancellationToken);
        
        var result = chats.Select(x => new LiveSessionChatResult
        {
            Id = x.Id,
            LiveSessionId = x.LiveSessionId,
            UserId = x.UserId,
            UserName = x.UserName,
            Message = x.Message,
            AvatarUrl = x.AvatarUrl,
            CreatedAt = x.CreatedAt
        }).ToList();

        return Result<List<LiveSessionChatResult>>.Success(result);
    }
}
