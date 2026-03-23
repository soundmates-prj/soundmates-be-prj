using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Commands.DeleteUserPlaylist;

public sealed class DeleteUserPlaylistHandler : ICommandHandler<DeleteUserPlaylistCommand>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;

    public DeleteUserPlaylistHandler(IUserPlaylistRepository userPlaylistRepository)
    {
        _userPlaylistRepository = userPlaylistRepository;
    }

    public async Task<Result> Handle(DeleteUserPlaylistCommand command, CancellationToken cancellationToken)
    {
        var playlist = await _userPlaylistRepository.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
        {
            return Result.Failure("Playlist not found", ErrorCode.NotFound);
        }

        if (playlist.UserId != command.UserId)
        {
            return Result.Failure("You do not have permission to delete this playlist", ErrorCode.Forbidden);
        }

        await _userPlaylistRepository.DeleteAsync(playlist, cancellationToken);
        return Result.Success();
    }
}
