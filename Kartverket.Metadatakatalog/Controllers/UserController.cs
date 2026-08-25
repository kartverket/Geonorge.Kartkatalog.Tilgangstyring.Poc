using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Kartverket.Metadatakatalog.Service;
using Kartverket.Metadatakatalog.Helpers;
using Microsoft.AspNetCore.Cors;

namespace Kartverket.Metadatakatalog.Controllers
{
    [ApiController]
    [EnableCors]
    [Route("api")]
    public class UserController(ILogger<HomeController> logger) : Controller
    {
        [HttpGet("user")]
        [ProducesResponseType(typeof(UserResult), 200)]
        public IActionResult GetUser()
        {
            logger.LogInformation("Get user called");
            UserResult result = new UserResult("Navn Navnesen", "navn@eksempel.no");

            return Ok(result);
        }
    }

    public class UserResult
    {
        public UserResult(string name, string email)
        {
            Name = name;
            Email = email;
        }

        public string Name { get; private set; }
        public string Email { get; private set; }
    }
}
