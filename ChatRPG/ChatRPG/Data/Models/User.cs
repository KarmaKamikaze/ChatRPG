﻿using Microsoft.AspNetCore.Identity;

namespace ChatRPG.Data.Models;

public class User : IdentityUser
{
    private User() {}

    public User(string username) : base(username) {}
}