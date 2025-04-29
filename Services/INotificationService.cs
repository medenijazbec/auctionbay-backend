using System.Collections.Generic;
using System.Threading.Tasks;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;

namespace auctionbay_backend.Services
{
    public interface INotificationService
    {
        Task CreateAsync(Notification n);
        Task<IEnumerable<Notification>> GetForUserAsync(string userId);
        Task MarkAsReadAsync(string userId, int notificationId);
        Task<int> GetUnreadCountAsync(string userId);

        Task MarkAllAsReadAsync(string userId);
    }
}
