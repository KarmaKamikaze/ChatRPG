using Microsoft.AspNetCore.Identity;

namespace ChatRPG.Data.Models;

public class User : IdentityUser
{
    public User()
    {
    }

    public User(string username) : base(username)
    {
    }

    public virtual ICollection<Campaign> Campaigns { get; } = new List<Campaign>();
}
