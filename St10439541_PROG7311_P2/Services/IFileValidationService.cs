namespace St10439541_PROG7311_P2.Services
{
    public interface IFileValidationService
    {
        (bool IsValid, string ErrorMessage) ValidatePdfFile(IFormFile file);
    }
}
