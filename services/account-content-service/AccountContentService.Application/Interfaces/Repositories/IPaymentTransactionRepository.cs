using AccountContentService.Application.Common.Pagination;
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

        Task<PaginationResult<PaymentTransaction>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
        Task<PaginationResult<PaymentTransaction>> GetByUserId(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
        /// <summary>
        /// Lấy thông tin giao dịch bằng ID duy nhất của bản ghi Giao dịch (Primary Key)
        /// Thường dùng khi đã biết chính xác ID cộng tác với bảng Transactions.
        /// </summary>
        Task<PaymentTransaction> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Lấy thông tin giao dịch bằng ID của bản ghi Thanh toán (Foreign Key)
        /// Rất hữu ích khi xử lý Callback từ các bên thứ 3 (VNPay) vì họ trả về mã tham chiếu thanh toán (vnp_TxnRef).
        /// Dùng để kiểm tra giao dịch đã được xử lý trước đó chưa (Idempotency).
        /// </summary>
        Task<PaymentTransaction> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);
    }
}
