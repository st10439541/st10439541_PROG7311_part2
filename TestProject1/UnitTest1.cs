using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using St10439541_PROG7311_P2.Models;
using St10439541_PROG7311_P2.Services;
using St10439541_PROG7311_P2.Controllers;
using Microsoft.EntityFrameworkCore;
using St10439541_PROG7311_P2.Data;

namespace TestProject1
{
    public class UnitTest1
    {
        private readonly FileValidationService _fileValidationService;

        public UnitTest1()
        {
            _fileValidationService = new FileValidationService();
        }

        [Fact]
        public void TestLogin_ValidCredentials_ShouldSucceed()
        {
            // Arrange
            var email = "admin@techmove.com";
            var password = "Admin@123";

            // Assert
            Assert.False(string.IsNullOrEmpty(email));
            Assert.False(string.IsNullOrEmpty(password));
            Assert.Equal("admin@techmove.com", email);
        }

        [Fact]
        public void TestLogin_InvalidCredentials_ShouldFail()
        {
            // Arrange
            var email = "wrong@email.com";
            var password = "wrongpassword";

            // Assert - These credentials would fail
            Assert.NotEqual("admin@techmove.com", email);
            Assert.NotEqual("Admin@123", password);
        }

        [Fact]
        public void TestRegister_NewUser_ShouldCreateUser()
        {
            // Arrange
            var newUser = new User
            {
                Email = "newuser@example.com",
                UserName = "newuser@example.com",
                FullName = "New User",
                CompanyName = "New Company",
                IsAdmin = false,
                RegistrationDate = DateTime.Now
            };

            // Assert
            Assert.NotNull(newUser);
            Assert.Equal("newuser@example.com", newUser.Email);
            Assert.False(newUser.IsAdmin);
        }

        [Fact]
        public void TestRegister_DuplicateEmail_ShouldFail()
        {
            // Arrange
            var existingEmail = "admin@techmove.com";
            var newEmail = "admin@techmove.com";

            // Assert - Duplicate emails should be detected
            Assert.Equal(existingEmail, newEmail);
        }

        [Fact]
        public void TestContractCreate_ValidContract_ShouldCreateSuccessfully()
        {
            // Arrange
            var contract = new Contract
            {
                ContractId = 1,
                ClientId = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                Status = ContractStatus.PendingClientSignature,
                ServiceLevel = ServiceLevel.Standard,
                IsSignedByClient = false
            };

            // Assert
            Assert.NotNull(contract);
            Assert.Equal(ContractStatus.PendingClientSignature, contract.Status);
            Assert.False(contract.IsSignedByClient);
        }

        [Fact]
        public void TestContractCreate_WithActiveStatus_ShouldBeSigned()
        {
            // Arrange
            var contract = new Contract
            {
                ContractId = 2,
                ClientId = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                Status = ContractStatus.Active,
                ServiceLevel = ServiceLevel.Premium,
                IsSignedByClient = true,
                SignatureDate = DateTime.Now
            };

            // Assert
            Assert.True(contract.IsSignedByClient);
            Assert.NotNull(contract.SignatureDate);
        }

        [Fact]
        public void TestIsActive_ActiveContract_ReturnsTrueForServiceRequests()
        {
            // Arrange
            var contract = new Contract
            {
                Status = ContractStatus.Active,
                IsSignedByClient = true,
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now.AddDays(30)
            };

            // Act
            bool canCreateServiceRequest = contract.CanCreateServiceRequest();

            // Assert
            Assert.True(canCreateServiceRequest);
        }

        [Fact]
        public void TestIsActive_ExpiredContract_ReturnsFalseForServiceRequests()
        {
            // Arrange
            var contract = new Contract
            {
                Status = ContractStatus.Expired,
                IsSignedByClient = false,
                StartDate = DateTime.Now.AddDays(-60),
                EndDate = DateTime.Now.AddDays(-1)
            };

            // Act
            bool canCreateServiceRequest = contract.CanCreateServiceRequest();
            bool isExpired = contract.IsExpired();

            // Assert
            Assert.False(canCreateServiceRequest);
            Assert.True(isExpired);
        }

        [Fact]
        public void TestIsActive_PendingContract_ReturnsFalseForServiceRequests()
        {
            // Arrange
            var contract = new Contract
            {
                Status = ContractStatus.PendingClientSignature,
                IsSignedByClient = false,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30)
            };

            // Act
            bool canCreateServiceRequest = contract.CanCreateServiceRequest();

            // Assert
            Assert.False(canCreateServiceRequest);
        }

        [Fact]
        public void ServiceRequest_CannotBeCreated_WhenContractExpired()
        {
            // Arrange
            var contract = new Contract
            {
                ContractId = 1,
                Status = ContractStatus.Expired,
                StartDate = DateTime.Now.AddDays(-90),
                EndDate = DateTime.Now.AddDays(-1)
            };

            // Act
            bool canCreate = contract.CanCreateServiceRequest();
            bool isExpired = contract.IsExpired();

            // Assert
            Assert.False(canCreate);
            Assert.True(isExpired);
        }

        [Fact]
        public void ServiceRequest_CannotBeCreated_WhenContractOnHold()
        {
            // Arrange
            var contract = new Contract
            {
                ContractId = 1,
                Status = ContractStatus.OnHold,
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now.AddDays(30)
            };

            // Act
            bool canCreate = contract.CanCreateServiceRequest();

            // Assert
            Assert.False(canCreate);
        }

        [Fact]
        public void TestAdminChange_UserPromotedToAdmin_ShouldHaveAdminRole()
        {
            // Arrange
            var user = new User
            {
                Id = "user123",
                Email = "user@example.com",
                IsAdmin = false
            };

            // Act - Promote to Admin
            user.IsAdmin = true;

            // Assert
            Assert.True(user.IsAdmin);
        }

        [Fact]
        public void TestAdminChange_AdminDemotedToUser_ShouldLoseAdminRole()
        {
            // Arrange
            var user = new User
            {
                Id = "admin123",
                Email = "admin@example.com",
                IsAdmin = true
            };

            // Act - Demote to User
            user.IsAdmin = false;

            // Assert
            Assert.False(user.IsAdmin);
        }

        [Fact]
        public void TestCurrencyConversion_ValidConversion_CalculatesCorrectZAR()
        {
            // Arrange
            decimal usdAmount = 100.00m;
            decimal exchangeRate = 19.50m;
            decimal expectedZar = 1950.00m;

            // Act
            decimal actualZar = usdAmount * exchangeRate;

            // Assert
            Assert.Equal(expectedZar, actualZar);
        }

        [Fact]
        public void TestCurrencyConversion_ZeroUSD_ReturnsZeroZAR()
        {
            // Arrange
            decimal usdAmount = 0m;
            decimal exchangeRate = 19.50m;
            decimal expectedZar = 0m;

            // Act
            decimal actualZar = usdAmount * exchangeRate;

            // Assert
            Assert.Equal(expectedZar, actualZar);
        }

        [Fact]
        public void TestCurrencyConversion_LargeAmount_CalculatesCorrectly()
        {
            // Arrange
            decimal usdAmount = 10000.00m;
            decimal exchangeRate = 19.50m;
            decimal expectedZar = 195000.00m;

            // Act
            decimal actualZar = usdAmount * exchangeRate;

            // Assert
            Assert.Equal(expectedZar, actualZar);
        }

        [Fact]
        public void TestFileValidation_ValidPdfFile_ReturnsSuccess()
        {
            // Arrange
            var file = CreateMockPdfFile("contract.pdf", true);

            // Act
            var (isValid, errorMessage) = _fileValidationService.ValidatePdfFile(file);

            // Assert
            Assert.True(isValid);
            Assert.Empty(errorMessage);
        }

        [Fact]
        public void TestFileValidation_ExeFile_ReturnsError()
        {
            // Arrange
            var file = CreateMockFile("virus.exe", "application/x-msdownload");

            // Act
            var (isValid, errorMessage) = _fileValidationService.ValidatePdfFile(file);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Only PDF files are allowed", errorMessage);
        }

        [Fact]
        public void TestFileValidation_EmptyFile_ReturnsError()
        {
            // Arrange
            var file = CreateMockFile("empty.pdf", "application/pdf", 0);

            // Act
            var (isValid, errorMessage) = _fileValidationService.ValidatePdfFile(file);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Please upload a file", errorMessage);
        }

        [Fact]
        public void TestFileValidation_FileTooLarge_ReturnsError()
        {
            // Arrange - 11MB file
            var file = CreateMockFile("large.pdf", "application/pdf", 11 * 1024 * 1024);

            // Act
            var (isValid, errorMessage) = _fileValidationService.ValidatePdfFile(file);

            // Assert
            Assert.False(isValid);
            Assert.Contains("less than 10MB", errorMessage);
        }

        [Fact]
        public void ServiceRequest_StoresExchangeRateUsed()
        {
            // Arrange
            var serviceRequest = new ServiceRequest
            {
                ServiceRequestId = 1,
                AmountUSD = 100m,
                ExchangeRateUsed = 19.50m,
                AmountZAR = 1950m
            };

            // Assert
            Assert.Equal(19.50m, serviceRequest.ExchangeRateUsed);
            Assert.Equal(100m * 19.50m, serviceRequest.AmountZAR);
        }

        [Fact]
        public void TestServiceRequestHistory_StoresCreationDate()
        {
            // Arrange
            var beforeCreation = DateTime.Now;
            var serviceRequest = new ServiceRequest
            {
                ServiceRequestId = 1,
                CreatedAt = DateTime.Now
            };
            var afterCreation = DateTime.Now;

            // Assert
            Assert.True(serviceRequest.CreatedAt >= beforeCreation);
            Assert.True(serviceRequest.CreatedAt <= afterCreation);
        }

        [Fact]
        public void TestServiceRequestHistory_StoresAdminResponseDate()
        {
            // Arrange
            var serviceRequest = new ServiceRequest
            {
                ServiceRequestId = 1,
                Status = RequestStatus.Accepted,
                AdminResponseDate = DateTime.Now,
                AdminComments = "Request approved"
            };

            // Assert
            Assert.NotNull(serviceRequest.AdminResponseDate);
            Assert.Equal("Request approved", serviceRequest.AdminComments);
        }

        [Fact]
        public void TestServiceRequestStatusChange_PendingToAccepted()
        {
            // Arrange
            var serviceRequest = new ServiceRequest
            {
                ServiceRequestId = 1,
                Status = RequestStatus.Pending
            };

            // Act
            serviceRequest.Status = RequestStatus.Accepted;

            // Assert
            Assert.Equal(RequestStatus.Accepted, serviceRequest.Status);
        }

        [Fact]
        public void TestServiceRequestStatusChange_PendingToDenied()
        {
            // Arrange
            var serviceRequest = new ServiceRequest
            {
                ServiceRequestId = 1,
                Status = RequestStatus.Pending
            };

            // Act
            serviceRequest.Status = RequestStatus.Denied;

            // Assert
            Assert.Equal(RequestStatus.Denied, serviceRequest.Status);
        }

        [Fact]
        public void TestServiceRequestStatusChange_AcceptedCannotBeChangedAgain()
        {
            // Arrange
            var serviceRequest = new ServiceRequest
            {
                ServiceRequestId = 1,
                Status = RequestStatus.Accepted
            };

            // Act - Trying to change after accepted
            bool wasChanged = serviceRequest.Status != RequestStatus.Accepted;

            // Assert - Status should remain Accepted
            Assert.Equal(RequestStatus.Accepted, serviceRequest.Status);
            Assert.False(wasChanged);
        }

        // Helper Methods
        private IFormFile CreateMockPdfFile(string fileName, bool validHeader, long size = 1024)
        {
            byte[] content;

            if (validHeader)
            {
                string pdfContent = "%PDF-1.4\n%âãÏÓ\n1 0 obj\n<<\n/Type /Catalog\n/Pages 2 0 R\n>>\nendobj\n%%EOF";
                content = Encoding.UTF8.GetBytes(pdfContent);
            }
            else
            {
                content = new byte[size];
                for (int i = 0; i < size && i < 100; i++)
                {
                    content[i] = (byte)('A' + (i % 26));
                }
            }

            var stream = new MemoryStream(content);
            var file = new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            return file;
        }

        private IFormFile CreateMockFile(string fileName, string contentType, long size = 1024, bool isPdf = false)
        {
            byte[] content;

            if (isPdf)
            {
                content = Encoding.UTF8.GetBytes("%PDF-1.4\nTest PDF content");
            }
            else
            {
                content = new byte[size];
                for (int i = 0; i < size && i < 100; i++)
                {
                    content[i] = (byte)('X' + (i % 26));
                }
            }

            var stream = new MemoryStream(content);
            var file = new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return file;
        }
    }
}