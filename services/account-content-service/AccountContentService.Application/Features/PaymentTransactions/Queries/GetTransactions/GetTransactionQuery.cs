using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.PaymentTransactions.Queries.GetTransactions
{
    public class GetTransactionsQuery : IRequest<PaginationResult<TransactionDto>>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GetTransactionByIdQuery : IRequest<TransactionDto>
    {
        public Guid Id { get; set; }
        public GetTransactionByIdQuery(Guid id)
        {
            Id = id;
        }
    }

    public class GetTransactionByUserIdQuery : IRequest<PaginationResult<TransactionDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
