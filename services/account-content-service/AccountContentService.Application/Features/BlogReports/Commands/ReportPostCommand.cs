using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Features.BlogReports.Commands;

public class ReportPostCommand : IRequest<BlogReportDto>
{
    public Guid BlogPostId { get; set; }
    public Guid ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ReportPostCommandHandler : IRequestHandler<ReportPostCommand, BlogReportDto>
{
    private readonly IBlogReportRepository _reportRepository;
    private readonly IBlogPostRepository _postRepository;
    private readonly IMapper _mapper;

    public ReportPostCommandHandler(
        IBlogReportRepository reportRepository, 
        IBlogPostRepository postRepository, 
        IMapper mapper)
    {
        _reportRepository = reportRepository;
        _postRepository = postRepository;
        _mapper = mapper;
    }

    public async Task<BlogReportDto> Handle(ReportPostCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if post exists
        var post = await _postRepository.GetByIdAsync(request.BlogPostId, cancellationToken);

        if (post == null)
            throw new NotFoundException("Blog post not found.");

        // 2. Prevent reporting own post
        if (post.UserId == request.ReporterUserId)
            throw new ValidationException("You cannot report your own post.");

        // 3. Check for existing report
        var existingReport = await _reportRepository.GetByUserAndPostAsync(
            request.ReporterUserId, 
            request.BlogPostId, 
            cancellationToken);

        if (existingReport != null)
        {
            existingReport.Reason = request.Reason;
            existingReport.Description = request.Description;
            existingReport.UpdatedAt = DateTime.UtcNow;
            await _reportRepository.UpdateAsync(existingReport, cancellationToken);
            return _mapper.Map<BlogReportDto>(existingReport);
        }

        // 4. Create new report
        var report = new BlogReport
        {
            BlogPostId = request.BlogPostId,
            ReporterUserId = request.ReporterUserId,
            Reason = request.Reason,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // Fixed: pass cancellationToken
        await _reportRepository.AddAsync(report, cancellationToken);

        return _mapper.Map<BlogReportDto>(report);
    }
}