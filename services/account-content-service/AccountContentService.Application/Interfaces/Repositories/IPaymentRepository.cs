using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface IPaymentRepository 
    {

        /// <summary>
        /// CRUD operations for payments
        ///</summary>
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task DeleteAsync(Payment payment);

        Task<Payment> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<Payment?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken);
    }
}
