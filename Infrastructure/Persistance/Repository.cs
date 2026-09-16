using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Infrastructure.Persistance
{
    public abstract class Repository
    {
        protected readonly SiGAVContext _context;
        protected readonly IDbConnection _connection;
        public Repository(SiGAVContext context)
        {
            _context = context;
            _connection = _context.Database.GetDbConnection();
        }
    }
}
