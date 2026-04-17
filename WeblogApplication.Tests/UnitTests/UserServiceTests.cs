using Microsoft.EntityFrameworkCore;
using Moq;
using WeblogApplication.Data;
using WeblogApplication.Implementation;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;
using Xunit;

namespace WeblogApplication.Tests.UnitTests
{
    public class UserServiceTests
    {
        private WeblogApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<WeblogApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new WeblogApplicationDbContext(options);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUserWithBloggerRole()
        {
            // Arrange
            var context = GetDbContext();
            var emailServiceMock = new Mock<IEmailService>();
            var service = new UserService(context, emailServiceMock.Object);
            var email = "test@example.com";
            var username = "testuser";
            var password = "Password123!";

            // Act
            var user = await service.RegisterAsync(email, username, password);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
            Assert.Equal(username, user.Username);
            Assert.Equal(UserRole.Blogger, user.Role);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, user.Password));
            
            var savedUser = await context.Users.FindAsync(user.Id);
            Assert.NotNull(savedUser);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnUser_WhenCredentialsAreValid()
        {
            // Arrange
            var context = GetDbContext();
            var emailServiceMock = new Mock<IEmailService>();
            var service = new UserService(context, emailServiceMock.Object);
            var password = "SecurePassword123!";
            var user = new UserModel
            {
                Email = "auth@test.com",
                Username = "authuser",
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Blogger
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await service.AuthenticateAsync("auth@test.com", password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsInvalid()
        {
            // Arrange
            var context = GetDbContext();
            var emailServiceMock = new Mock<IEmailService>();
            var service = new UserService(context, emailServiceMock.Object);
            var user = new UserModel
            {
                Email = "auth@test.com",
                Username = "authuser",
                Password = BCrypt.Net.BCrypt.HashPassword("RealPassword"),
                Role = UserRole.Blogger
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await service.AuthenticateAsync("auth@test.com", "WrongPassword");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldUpdateUserProperties()
        {
            // Arrange
            var context = GetDbContext();
            var emailServiceMock = new Mock<IEmailService>();
            var service = new UserService(context, emailServiceMock.Object);
            var user = new UserModel { Email = "u@t.com", Username = "old", Role = UserRole.Blogger, Password = "password" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var success = await service.UpdateProfileAsync(user.Id, "newname", "newbio");

            // Assert
            Assert.True(success);
            var updated = await context.Users.FindAsync(user.Id);
            Assert.Equal("newname", updated!.Username);
            Assert.Equal("newbio", updated.Bio);
        }
    }
}
