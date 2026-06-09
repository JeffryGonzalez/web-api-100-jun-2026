using System;
using System.Collections.Generic;
using System.Text;

namespace DemoStuff2;

public record Pet 
{
    public required string Name { get; init; } = string.Empty;
    public string Breed { get; init;  } = string.Empty;

    
}
