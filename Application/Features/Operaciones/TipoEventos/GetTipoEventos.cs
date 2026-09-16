using Application.Features.Misc;
using Domain.ViewModels;
using Infrastructure.Persistance.Misc;
using MediatR;

namespace Application.Features.Operaciones.TipoEventos;
public record GetTipoEventosQuery : IRequest<IEnumerable<NamedViewModel>>;
{

}

public class GetTipoEventosQueryHandler(IMiscRepository repository) : IRequestHandler<GetTipoEventosQuery, IEnumerable<NamedViewModel>>
{
    public async Task<IEnumerable<NamedViewModel>> Handle(GetTipoEventosQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetNamedResource("TipoEventos");
    }
}