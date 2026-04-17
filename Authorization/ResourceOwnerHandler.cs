using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using WeblogApplication.Models;
using System.IdentityModel.Tokens.Jwt;

namespace WeblogApplication.Authorization
{
    public class ResourceOwnerHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            var pendingRequirements = context.PendingRequirements.ToList();

            foreach (var requirement in pendingRequirements)
            {
                if (requirement is ResourceOwnerRequirement)
                {
                    if (context.Resource is BlogModel blog)
                    {
                        if (IsOwner(context.User, blog.UserId))
                        {
                            context.Succeed(requirement);
                        }
                    }
                    else if (context.Resource is CommentModel comment)
                    {
                        if (IsOwner(context.User, comment.UserId))
                        {
                            context.Succeed(requirement);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private bool IsOwner(ClaimsPrincipal user, int resourceUserId)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId == resourceUserId;
            }

            return false;
        }
    }
}
