using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auctionbay_backend.Data;
using auctionbay_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace auctionbay_backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        public NotificationService(ApplicationDbContext db) => _db = db;

        public async Task CreateAsync(Notification n)
        {
            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();
        }

        public Task<IEnumerable<Notification>> GetForUserAsync(string userId)
            => _db.Notifications
                  .Where(n => n.UserId == userId)
                  .OrderByDescending(n => n.Timestamp)
                  .AsNoTracking()
                  .ToListAsync()
                  .ContinueWith(t => t.Result.AsEnumerable());

        public async Task MarkAsReadAsync(string userId, int notificationId)
        {
            var n = await _db.Notifications
                             .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);
            if (n != null && !n.IsRead)
            {
                n.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }
        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _db.Notifications
              .Where(n => n.UserId == userId && !n.IsRead)
              .ToListAsync();

            notifications.ForEach(n => n.IsRead = true);

            await _db.SaveChangesAsync();
        }


        public Task<int> GetUnreadCountAsync(string userId)
            => _db.Notifications
                  .Where(n => n.UserId == userId && !n.IsRead)
                  .CountAsync();
    }
}
