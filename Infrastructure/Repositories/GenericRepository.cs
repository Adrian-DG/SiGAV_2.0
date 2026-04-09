using Infrastructure.Data;

namespace Infrastructure.Repositories;

public abstract class GenericRepository(SiGAVContext context)
{
    protected readonly SiGAVContext _context = context;
}