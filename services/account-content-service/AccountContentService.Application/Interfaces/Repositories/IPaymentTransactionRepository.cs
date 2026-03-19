using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface IPaymentTransactionRepository
    {
        /// <summary>
        /// CRUD operations for payment transactions
        ///</summary>
        Task AddAsync(PaymentTransaction paymentTransaction);
        Task UpdateAsync(PaymentTransaction paymentTransaction);
        Task DeleteAsync(PaymentTransaction paymentTransaction);
    }
}
