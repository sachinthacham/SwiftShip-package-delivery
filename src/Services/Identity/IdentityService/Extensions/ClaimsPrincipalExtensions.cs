using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdentityService.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token does not contain a subject claim.");

        return Guid.Parse(subject);
    }
}
