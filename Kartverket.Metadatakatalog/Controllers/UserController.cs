using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Kartverket.Metadatakatalog.Service;
using Kartverket.Metadatakatalog.Helpers;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Primitives;

namespace Kartverket.Metadatakatalog.Controllers
{
    [ApiController]
    [Route("api")]
    [EnableCors]
    public class UserController(ILogger<UserController> logger) : Controller
    {
        [HttpGet("user")]
        [ProducesResponseType(typeof(UserResult), 200)]
        public IActionResult GetUser()
        {
            logger.LogInformation("Get user called");

            //Ikke helt safe for injection og bruk rett fra headeren, men for POC er det godt nok
            Request.Headers.TryGetValue("zt-name", out StringValues ztName);

            string name = "Navn Navnesen";
            
            if (!String.IsNullOrEmpty(ztName[0]))
            {
                name = ztName[0];
            }
            
            UserResult result = new UserResult(name, "navn@eksempel.no");

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
