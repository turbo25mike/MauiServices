using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Turbo.Maui.Services.Examples.Shared.Models;
using Microsoft.AspNetCore.Authorization;

namespace Turbo.Maui.Services.Examples.API.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    [HttpGet]
    [Route("")]
    public IEnumerable<ShortUser> Get() => Avengers.Team.Assemble();

    [HttpGet]
    [Route("{id}")]
    public User? Get(string id) => Avengers.Team.Get(id);

    [HttpPost]
    [Authorize]
    [Route("")]
    public void Post([FromBody] User user) => Avengers.Team.Add(user);

    [HttpPut]
    [Authorize]
    [Route("{id}")]
    public void Put(string id, [FromBody] JsonPatchDocument<User> data) => Avengers.Team.Update(id, data);

    [HttpDelete]
    [Authorize]
    [Route("{id}")]
    public void Delete(string id) => Avengers.Team.Delete(id);
}

