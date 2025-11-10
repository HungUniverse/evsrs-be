using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Repository
{
    public class CarEVRepository : GenericRepository<CarEV> , ICarEVRepository
    {
        public CarEVRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
        public async Task CreateCarEVAsync(CarEV carEV)
        {
            await InsertAsync(carEV);


        }

        public async Task DeleteCarEVAsync(CarEV carEV)
        {
            await DeleteAsync(carEV);
        }

        

        public async Task<CarEV?> GetCarEVByIdAsync(string id)
        {
            var response = await _dbSet
                .Include(c => c.Model)
                    .ThenInclude(m => m.CarManufacture)
                .Include(c => c.Model)
                    .ThenInclude(m => m.Amenities)
                .Include(c => c.Depot)
                .Where(x => !x.IsDeleted && x.Id == id)
                .FirstOrDefaultAsync();
            return response;
        }

        public async Task<PaginatedList<CarEV>> GetCarEVList()
        {
            var response = await _dbSet
                .Include(c => c.Model)
                    .ThenInclude(m => m.CarManufacture)
                .Include(c => c.Model)
                    .ThenInclude(m => m.Amenities)
                .Include(c => c.Depot)
                .Where(x => !x.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
            return PaginatedList<CarEV>.Create(response, 1, response.Count);
        }

        public async Task<List<CarEV>> GetAllCarEVsByDepotIdAsync(string depotId)
        {
            return await _dbSet
                .Include(c => c.Model)
                    .ThenInclude(m => m.CarManufacture)
                .Include(c => c.Model)
                    .ThenInclude(m => m.Amenities)
                .Include(c => c.Depot)
                .Where(c => c.DepotId == depotId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaginatedList<CarEV>> GetCarEVsByDepotIdAsync(string depotId, int pageNumber, int pageSize)
        {
            var query = _context.CarEVs
                .Include(c => c.Model)
                    .ThenInclude(m => m.CarManufacture)
                .Include(c => c.Model)
                    .ThenInclude(m => m.Amenities)
                .Include(c => c.Depot)
                .Where(c => c.DepotId == depotId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt);

            return await PaginatedList<CarEV>.CreateAsync(query, pageNumber, pageSize);
        }

        

        public async Task UpdateCarEVAsync(CarEV carEV)
        {
            await UpdateAsync(carEV);
        }

        public async Task<List<CarEV>> GetAvailableCarsByModelAndDepotAsync(string modelId, string depotId)
        {
            return await _dbSet
                .Include(c => c.Model)
                    .ThenInclude(m => m!.CarManufacture)
                .Include(c => c.Model)
                    .ThenInclude(m => m!.Amenities)
                .Include(c => c.Depot)
                .Where(c => !c.IsDeleted && 
                           c.ModelId == modelId && 
                           c.DepotId == depotId &&
                           c.Status == BusinessObjects.Enum.CarEvStatus.AVAILABLE)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<CarEV?> FindAndLockAvailableCarAsync(string modelId, string depotId, DateTime startDate, DateTime endDate, int bufferMinutes)
        {
            // 🔒 CRITICAL: Use SELECT ... FOR UPDATE to prevent race condition
            // Khi 2 khách cùng đặt xe online cùng lúc, chỉ 1 người được lock xe, người kia phải đợi
            
            // Lấy danh sách xe của model tại depot (với lock)
            var availableCars = await _dbSet
                .Where(c => !c.IsDeleted && 
                           c.ModelId == modelId && 
                           c.DepotId == depotId &&
                           c.Status == BusinessObjects.Enum.CarEvStatus.AVAILABLE)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            if (!availableCars.Any())
                return null;

            // Kiểm tra từng xe xem có booking conflict không (bao gồm buffer time)
            foreach (var car in availableCars)
            {
                // Check có booking nào conflict không
                var conflictingBookings = await _context.Bookings
                    .Where(x => !x.IsDeleted &&
                               x.CarEVDetailId == car.Id &&
                               x.Status != BusinessObjects.Enum.OrderBookingStatus.CANCELLED &&
                               x.Status != BusinessObjects.Enum.OrderBookingStatus.COMPLETED &&
                               x.Status != BusinessObjects.Enum.OrderBookingStatus.RETURNED)
                    .ToListAsync();

                bool hasConflict = false;
                foreach (var booking in conflictingBookings)
                {
                    if (!booking.StartAt.HasValue || !booking.EndAt.HasValue)
                        continue;

                    // Thêm buffer vào thời gian của booking hiện tại
                    var existingStartWithBuffer = booking.StartAt.Value.AddMinutes(-bufferMinutes);
                    var existingEndWithBuffer = booking.EndAt.Value.AddMinutes(bufferMinutes);
                    
                    // Kiểm tra overlap
                    if (startDate <= existingEndWithBuffer && endDate >= existingStartWithBuffer)
                    {
                        hasConflict = true;
                        break;
                    }
                }

                if (!hasConflict)
                {
                    // ✅ Tìm thấy xe available → Return luôn (xe này đã được lock trong transaction)
                    return car;
                }
            }

            return null; // Không có xe nào available
        }
    }
}
