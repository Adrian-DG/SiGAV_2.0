using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{

    [ApiController]
    public abstract class GenericController(IMediator mediator) : ControllerBase
    {
        protected readonly IMediator Mediator = mediator;
    }
}
