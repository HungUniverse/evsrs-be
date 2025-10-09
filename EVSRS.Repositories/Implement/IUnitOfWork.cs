using System;
using EVSRS.Repositories.Interface;

namespace EVSRS.Repositories.Implement
{
    public interface IUnitOfWork
    {
        IUserRepository UserRepository { get; }
        ITokenRepository TokenRepository { get; }
        IOTPRepository OTPRepository { get; }
        ICarManufactureRepository CarManufactureRepository { get; }
        IModelRepository ModelRepository { get; }
        ICarEVRepository CarEVRepository { get; }
        IAmenitiesRepository AmenitiesRepository { get; }
        IFeedbackRepository FeedbackRepository { get; }


        int SaveChanges();
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void CommitTransaction();
        void RollBack();
    }
}
