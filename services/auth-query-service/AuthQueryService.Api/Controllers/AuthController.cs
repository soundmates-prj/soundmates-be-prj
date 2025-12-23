using AuthQueryService.Application.DTOs.Request;
using AuthQueryService.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AuthQueryService.Application.DTOs.Response;
using System.Threading;
using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;

namespace AuthQueryService.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
    }
}
