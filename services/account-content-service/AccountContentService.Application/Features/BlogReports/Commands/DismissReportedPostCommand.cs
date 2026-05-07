using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Features.BlogReports.Commands;

public class DismissReportedPostCommand : IRequest<bool>
{
    public Guid BlogPostId { get; set; }

    public DismissReportedPostCommand(Guid blogPostId)
    {
        BlogPostId = blogPostId;
    }
}

public class DismissReportedPostCommandHandler : IRequestHandler<DismissReportedPostCommand, bool>
{
    private readonly IBlogPostRepository _postRepository;
    private readonly IBlogReportRepository _reportRepository;

    public DismissReportedPostCommandHandler(
        IBlogPostRepository postRepository,
        IBlogReportRepository reportRepository)
    {
        _postRepository = postRepository;
        _reportRepository = reportRepository;
    }

    public async Task<bool> Handle(DismissReportedPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.BlogPostId, cancellationToken);
        if (post == null)
            throw new NotFoundException("Blog post not found.");

        await _reportRepository.DeleteReportsByPostIdAsync(request.BlogPostId, cancellationToken);
        
        return true;
    }
}
