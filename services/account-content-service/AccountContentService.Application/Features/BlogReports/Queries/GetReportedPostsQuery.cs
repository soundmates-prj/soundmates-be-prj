using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;

namespace AccountContentService.Application.Features.BlogReports.Queries;

public class GetReportedPostsQuery : IRequest<IEnumerable<ReportedPostDto>> { }

public class GetReportedPostsQueryHandler : IRequestHandler<GetReportedPostsQuery, IEnumerable<ReportedPostDto>>
{
    private readonly IBlogReportRepository _repository;

    public GetReportedPostsQueryHandler(IBlogReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReportedPostDto>> Handle(GetReportedPostsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetReportedPostsAsync(cancellationToken);
    }
}