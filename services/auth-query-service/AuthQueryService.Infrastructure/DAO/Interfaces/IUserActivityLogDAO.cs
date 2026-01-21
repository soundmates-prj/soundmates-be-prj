using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Infrastructure.DAO.Interfaces
{
    // Alias for clean architecture - DAO implements Repository interface from Domain
    public interface IUserActivityLogDAO : IUserActivityLogRepository
    {
    }
}
