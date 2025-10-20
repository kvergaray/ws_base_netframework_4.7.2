using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsService.Domain;

namespace WindowsService.Infrastructure.DataAccess
{
    public interface IUserRepository
    {
        Task<IReadOnlyList<UserListarDto>> GetUsersToProcessAsync(int maxAttempts, CancellationToken cancellationToken);
    }
}
