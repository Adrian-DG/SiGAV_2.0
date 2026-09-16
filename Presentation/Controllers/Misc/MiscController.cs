using Application.Features.Misc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Misc
{
    [Route("api/resources")]
    public class MiscController : GenericController
    {
        public MiscController(IMediator mediator) : base(mediator)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetNamedResource([FromQuery] GetNamedResourceQuery query)
            => new JsonResult(await Mediator.Send(query));

    }
}
