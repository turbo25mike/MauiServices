using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Turbo.Maui.Services.Examples.Shared.Models;

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
    [Route("")]
    public void Post([FromBody] User user) => Avengers.Team.Add(user);

    [HttpPut]
    [Route("{id}")]
    public void Put(string id, [FromBody] JsonPatchDocument<User> data) => Avengers.Team.Update(id, data);

    [HttpDelete]
    [Route("{id}")]
    public void Delete(string id) => Avengers.Team.Delete(id);
}

