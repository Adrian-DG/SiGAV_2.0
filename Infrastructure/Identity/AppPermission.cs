using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class AppPermission : IdentityRole<int>
{
    public string? Description { get; set; }
}