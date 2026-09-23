using Application.Contracts;
using Dapper;
using Domain.ViewModels;

namespace Infrastructure.Persistance.Misc
{   
    public class MiscQueryService : QueryService, IMiscRepository
    {
        public MiscQueryService(SiGAVContext context) : base(context)
        {
        }

        public async Task<IEnumerable<NamedViewModel>> GetNamedResource(string column, string table, object? parameters = null)
                    => await _connection.QueryAsync<NamedViewModel>($@"SELECT Id, Nombre FROM {table} WHERE {column} = @Value ORDER BY Nombre ASC", parameters);         
    }
}
