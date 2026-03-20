using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace AccountContentService.Application.Features.PaymentTransactions.Queries.GetTransactions
{
    public class GetTransactionHandler 
        : IRequestHandler<GetTransactionByIdQuery, TransactionDto>,
          IRequestHandler<GetTransactionByUserIdQuery, PaginationResult<TransactionDto>>,
          IRequestHandler<GetTransactionsQuery, PaginationResult<TransactionDto>>
    {
        private readonly IPaymentTransactionRepository _repository;
        private readonly IMapper _mapper;

        public GetTransactionHandler(IPaymentTransactionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (result == null)
            {
                throw new NotFoundException("Transaction not found!");
            }

            return _mapper.Map<TransactionDto>(result);
        }

        public async Task<PaginationResult<TransactionDto>> Handle(GetTransactionByUserIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByUserId(request.UserId, request.Page, request.PageSize, cancellationToken);

            var items = _mapper.Map<IEnumerable<TransactionDto>>(result.Items).ToList();

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
            var result = await _repository.GetAllAsync(request.Page, request.PageSize, cancellationToken);

            var items = _mapper.Map<IEnumerable<TransactionDto>>(result.Items).ToList();

            return new PaginationResult<TransactionDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
