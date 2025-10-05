using System;
using EVSRS.BusinessObjects.DBContext;
using EVSRS.Repositories.Interface;

namespace EVSRS.Repositories.Implement;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public IUserRepository UserRepository { get; }

    public ITokenRepository TokenRepository { get; }
    public IOTPRepository OTPRepository { get; }

    public UnitOfWork(ApplicationDbContext context, IUserRepository userRepository, ITokenRepository tokenRepository, IOTPRepository otpRepository)
    {
        _context = context;
        UserRepository = userRepository;
        TokenRepository = tokenRepository;
        OTPRepository = otpRepository;
    }

    public int SaveChanges()
    {
        int result;

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                result = _context.SaveChanges();
                transaction.Commit();
            }
            catch (Exception)
            {
                result = -1;
                transaction.Rollback();
            }
        }

        return result;
    }

    public async Task<int> SaveChangesAsync()
    {
        int result;

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                result = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during SaveChangesAsync: {ex.Message}");
                result = -1;
                await transaction.RollbackAsync();
            }
        }
        return result;
    }


    public void BeginTransaction()
    {
        _context.Database.BeginTransaction();
    }

    public void CommitTransaction()
    {
        _context.Database.CommitTransaction();
    }

    public void RollBack()
    {
        _context.Database.RollbackTransaction();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
