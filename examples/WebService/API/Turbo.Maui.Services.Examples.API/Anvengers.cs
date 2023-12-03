using Microsoft.AspNetCore.JsonPatch;
using Turbo.Maui.Services.Examples.Shared.Models;

namespace Turbo.Maui.Services.Examples.API
{
    public sealed class Avengers
    {
        private Avengers()
        {
            //Default Team
            _Team = new()
            {
                new(){ AcceptedEulaVersion = 1, FirstName = "Thor", LastName = "Odinson" },
                new(){ AcceptedEulaVersion = 2, FirstName = "Natasha", LastName = "Rominoff" },
                new(){ AcceptedEulaVersion = 2, FirstName = "Tony", LastName = "Stark" },
                new(){ AcceptedEulaVersion = 2, FirstName = "Steve", LastName = "Rodgers" },
                new(){ AcceptedEulaVersion = 2, FirstName = "Clint", LastName = "Barton" },
                new(){ AcceptedEulaVersion = 2, FirstName = "Peter", LastName = "Parker" },
                new(){ AcceptedEulaVersion = 2, FirstName = "James", LastName = "Rhodes" },
                new(){ AcceptedEulaVersion = 2, FirstName = "Carol", LastName = "Danvers" }
            };
        }

        private static readonly Lazy<Avengers> _Lazy = new(() => new Avengers());
        public static Avengers Team => _Lazy.Value;

        public IEnumerable<ShortUser> Assemble() => _Team.Select(ShortUser.Create);

        public User Get(string id)
        {
            var user = _Team.FirstOrDefault(user => user.ID == id);
            return user is null ? throw new ArgumentOutOfRangeException("User not found") : user;
        }

        public void Add(User user) => _Team.Add(user);

        public void Update(string id, JsonPatchDocument<User> patch) => patch.ApplyTo(Get(id));

        public void Delete(string id) => _Team.Remove(Get(id));

        private readonly List<User> _Team;
    }
}