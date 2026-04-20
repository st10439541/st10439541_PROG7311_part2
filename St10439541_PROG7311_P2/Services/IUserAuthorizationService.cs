namespace St10439541_PROG7311_P2.Services
{
    public interface IUserAuthorizationService
    {
        bool IsAdmin();
        bool IsAuthenticated();
        Task<string?> GetCurrentUserId();
        Task<int?> GetCurrentUserClientId();
    }
}