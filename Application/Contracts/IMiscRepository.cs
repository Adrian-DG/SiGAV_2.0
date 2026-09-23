using Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts
{
    public interface IMiscRepository
    {
        Task<IEnumerable<NamedViewModel>> GetNamedResource(string column, string table, object? parameters = null);
    }
}
