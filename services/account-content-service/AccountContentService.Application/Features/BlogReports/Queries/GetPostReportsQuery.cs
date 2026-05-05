using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.BlogReports.Queries;

public class GetPostReportsQuery : IRequest<IEnumerable<BlogReportDto>>
{
    public Guid PostId { get; set; }
}

public class GetPostReportsQueryHandler : IRequestHandler<GetPostReportsQuery, IEnumerable<BlogReportDto>>
{
    private readonly IBlogReportRepository _repository;
    private readonly IUserProfileReadModelRepository _userProfileReadModelRepository;
    private readonly IMapper _mapper;

    public GetPostReportsQueryHandler(IBlogReportRepository repository, IMapper mapper, IUserProfileReadModelRepository userProfileReadModelRepository)
    {
        _repository = repository;
        _mapper = mapper;
        _userProfileReadModelRepository = userProfileReadModelRepository;

    }

    public async Task<IEnumerable<BlogReportDto>> Handle(GetPostReportsQuery request, CancellationToken cancellationToken)
    {
        var reports = await _repository.GetReportsByPostIdAsync(request.PostId, cancellationToken);

        var userIds = reports
            .Select(r => r.ReporterUserId)
            .Distinct()
            .ToList();

        var users = await _userProfileReadModelRepository
            .GetByIdsAsync(userIds, cancellationToken);

        var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

        var result = reports.Select(report =>
        {
            var dto = _mapper.Map<BlogReportDto>(report);
            dto.ReporterUserName = userDict.TryGetValue(report.ReporterUserId, out var name)
                ? name
                : "Unknown User";

            return dto;
        });

        return result;
    }
}