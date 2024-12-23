﻿namespace ChatRPG.Data.Models;

public class Environment
{
    private Environment()
    {
    }

    public Environment(Campaign campaign, string name, string description)
    {
        Campaign = campaign;
        Name = name;
        Description = description;
    }

    public int Id { get; private set; }
    public Campaign Campaign { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Description { get; set; } = null!;
}
