using Application.Contracts;
using Domain.ViewModels;
using MediatR;

namespace Application.Features.Operaciones.TipoEventos;

public record GetTipoEventosQuery : IRequest<IEnumerable<NamedViewModel>>;

public class GetTipoEventosQueryHandler(IMiscRepository repository) : IRequestHandler<GetTipoEventosQuery, IEnumerable<NamedViewModel>>
{
    public async Task<IEnumerable<NamedViewModel>> Handle(GetTipoEventosQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetNamedResource("IsActive", "tipo_eventos", new { Value = true });
    }
}
