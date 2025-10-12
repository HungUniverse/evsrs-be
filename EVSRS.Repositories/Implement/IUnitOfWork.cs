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
        IDepotRepository DepotRepository { get; }
        ITransactionRepository TransactionRepository { get; }
        IOrderBookingRepository OrderRepository { get; }
        IHandoverInspectionRepository HandoverInspectionRepository { get; }
        IReturnSettlementRepository ReturnSettlementRepository { get; }
        IContractRepository ContractRepository { get; }
        IIdentifyDocumentRepository IdentifyDocumentRepository { get; }

        int SaveChanges();
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void CommitTransaction();
        void RollBack();
    }
}
