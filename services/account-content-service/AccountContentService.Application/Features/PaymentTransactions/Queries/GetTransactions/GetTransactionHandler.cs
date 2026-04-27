using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.PaymentTransactions.Queries.GetTransactions
{
    public class GetTransactionHandler
        : IRequestHandler<GetTransactionByIdQuery, TransactionDto>,
          IRequestHandler<GetTransactionByUserIdQuery, PaginationResult<TransactionDto>>,
          IRequestHandler<GetTransactionsQuery, PaginationResult<TransactionDto>>
    {
        private readonly IPaymentTransactionRepository _repository;
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMapper _mapper;

        public GetTransactionHandler(
            IPaymentTransactionRepository repository,
            IUserProfileCache userProfileCache,
            IMapper mapper)
        {
            _repository = repository;
            _userProfileCache = userProfileCache;
            _mapper = mapper;
        }

        public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (result == null)
            {
                throw new NotFoundException("Transaction not found!");
            }

            var dto = _mapper.Map<TransactionDto>(result);

            // Populate TargetType/TargetId from Payment
            dto.TargetType = result.Payment?.TargetType ?? string.Empty;
            dto.TargetId = result.Payment?.TargetId;

            // Populate userProfile from local read-model (IUserProfileCache)
            dto.userProfile = await PopulateUserProfileAsync(result, dto, cancellationToken);

            return dto;
        }

        public async Task<PaginationResult<TransactionDto>> Handle(GetTransactionByUserIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByUserId(request.UserId, request.Page, request.PageSize, cancellationToken);

            var items = _mapper.Map<IEnumerable<TransactionDto>>(result.Items).ToList();

            // Populate userProfile and TargetType/TargetId for each transaction
            foreach (var (item, idx) in items.Select((x, i) => (x, i)))
            {
                var raw = result.Items.ElementAt(idx);
                item.TargetType = raw.Payment?.TargetType ?? string.Empty;
                item.TargetId = raw.Payment?.TargetId;
                item.userProfile = await PopulateUserProfileAsync(raw, item, cancellationToken);
            }

            return new PaginationResult<TransactionDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PaginationResult<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
        {
            // Use explicit join to safely get UserId from Payment table
            var result = await _repository.GetAllWithPaymentAsync(request.Page, request.PageSize, cancellationToken);

            var items = _mapper.Map<IEnumerable<TransactionDto>>(result.Items).ToList();

            // Populate userProfile and TargetType/TargetId for each transaction
            foreach (var (item, idx) in items.Select((x, i) => (x, i)))
            {
                var raw = result.Items.ElementAt(idx);
                item.TargetType = raw.Payment?.TargetType ?? string.Empty;
                item.TargetId = raw.Payment?.TargetId;
                item.userProfile = await PopulateUserProfileAsync(raw, item, cancellationToken);
            }

            return new PaginationResult<TransactionDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// Gets the user profile from the local read-model projection and
        /// attaches it to the TransactionDto. Falls back to UserId-only
        /// data if the projection is not yet available.
        /// </summary>
        private async Task<UserProfileDto> PopulateUserProfileAsync(
            Domain.Entities.PaymentTransaction transaction,
            TransactionDto dto,
            CancellationToken ct)
        {
            // Transaction → Payment → UserId
            var userId = transaction.Payment?.UserId ?? Guid.Empty;
            if (userId == Guid.Empty) return new UserProfileDto();

            var profile = await _userProfileCache.GetProfileAsync(userId, ct);

            // Split FullName → FirstName / LastName (best effort)
            var nameParts = (profile.FullName ?? "Unknown User").Split(' ', 2);
            return new UserProfileDto
            {
                Id = profile.UserId,
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                Email = string.Empty,
                ProfileImageUrl = profile.AvatarUrl ?? string.Empty
            };
        }
    }
}
