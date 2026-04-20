using Microsoft.AspNetCore.Identity;
using St10439541_PROG7311_P2.Models;

namespace St10439541_PROG7311_P2.Services
{
    public class UserAuthorizationService : IUserAuthorizationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;

        public UserAuthorizationService(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public bool IsAdmin()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.IsInRole("Admin") == true;
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }

        public async Task<string?> GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var currentUser = await _userManager.GetUserAsync(user);
            return currentUser?.Id;
        }

        public async Task<int?> GetCurrentUserClientId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var currentUser = await _userManager.GetUserAsync(user);
            return currentUser?.ClientId;
        }
    }
}