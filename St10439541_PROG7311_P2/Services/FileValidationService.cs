using System.IO;

namespace St10439541_PROG7311_P2.Services
{
    public class FileValidationService : IFileValidationService
    {
        private static readonly string[] AllowedExtensions = { ".pdf" };
        private const int MaxFileSizeMB = 10;
        private const int MaxFileSizeBytes = MaxFileSizeMB * 1024 * 1024;

        public (bool IsValid, string ErrorMessage) ValidatePdfFile(IFormFile file)
        {
            // Check 1: File exists
            if (file == null || file.Length == 0)
            {
                return (false, "Please upload a file.");
            }

            // Check 2: File extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return (false, "Only PDF files are allowed.");
            }

            // Check 3: File size
            if (file.Length > MaxFileSizeBytes)
            {
                return (false, $"File size must be less than {MaxFileSizeMB}MB.");
            }

            // Check 4: PDF header 
            using var stream = file.OpenReadStream();
            byte[] header = new byte[4];
            stream.Read(header, 0, 4);

            // PDF files must start with "%PDF"
            if (header[0] != 0x25 || header[1] != 0x50 ||
                header[2] != 0x44 || header[3] != 0x46)
            {
                return (false, "The file is not a valid PDF document.");
            }

            // All validations passed
            return (true, string.Empty);
        }
    }
}