using Microsoft.AspNetCore.Mvc;
using WeblogApplication.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using WeblogApplication.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

public class UserController : Controller
{
    private readonly WeblogApplicationDbContext _context;
    private readonly SmtpOptions _smtpOptions;

    public UserController(WeblogApplicationDbContext context, IOptions<SmtpOptions> smtpOptions)
    {
        _context = context;
        _smtpOptions = smtpOptions.Value;
    }

    [HttpGet]
    public IActionResult SignUp()
    {
        var model = new UserModel(); // Initialize UserModel with default values
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignUp(UserModel model)
    {
        ModelState.Remove("passwordResetToken");
        ModelState.Remove("Blogs");
        ModelState.Remove("Comments");
        if (ModelState.IsValid)
        {
            // Check if the username already exists
            if (_context.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError(nameof(UserModel.Username), "Username already exists.");
                return View(model);
            }

            // Check if the email already exists
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(UserModel.Email), "Email already exists.");
                return View(model);
            }

            // Hash the password before storing
            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            model.Role = UserRole.Blogger; // Force role to Blogger for public signup

            // Add the user to the database
            _context.Users.Add(model);
            _context.SaveChanges();

            // Set a success message to be shown on the login page
            TempData["SignUpSuccessMessage"] = "Successfully signed up!";
            TempData["AdminCreated"] = "Successfully Created User!";
            if (model.Role == UserRole.Admin) return Redirect("/admin");

            // Redirect to the login page
            return RedirectToAction("Login", "User");
        }

        // If the model state is not valid, return the view with validation errors
        return View(model);
    }


    public IActionResult SignUpSuccess()
    {
        return View();
    }
    [HttpGet]
    public IActionResult Login()
    {
        var model = new LoginModel(); // Initialize LoginViewModel with default values
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (ModelState.IsValid)
        {
            // Find the user by username
            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                if(user.Role.ToString() == "Admin") TempData["AdminLoginMessage"] = "Successfully Logged In";

                else TempData["AdminLoginMessage"] = "Successfully Logged In";

                // User found and password matches, set session data
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role.ToString());

                // Sign in via cookie authentication so [Authorize] works for MVC
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                };
                var identity = new ClaimsIdentity(claims, "SessionAuth");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("SessionAuth", principal);

                if (user.Role == UserRole.Admin) return Redirect("/admin");
                return Redirect("/blog");
            }

            // User not found or password does not match
            TempData["LoginFailureMessage"] = "Invalid username or password";
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }
        // If the model state is not valid, return the view with validation errors
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        // Clear session data
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync("SessionAuth");

        // Redirect to the home page or login page
        return RedirectToAction("Index", "Blog");
    }

    public async Task<IActionResult> EditProfile()
    {
        // Server-side authentication check
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Login", "User");
        }

        // Get the user's email from session data
        var userEmail = HttpContext.Session.GetString("Email");

        // Query the database to find the user profile
        var userProfile = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

        if (userProfile == null)
        {
            // Handle the case where the user profile is not found
            return NotFound();
        }
        

        return View(userProfile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UserModel model)
    {
        // Server-side authentication check
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Login", "User");
        }

        if (String.IsNullOrEmpty(model.Password)) ModelState.Remove("Password");
        ModelState.Remove("Blogs");
            ModelState.Remove("Comments");
                if (!ModelState.IsValid)
        {
            // If the model state is not valid, return the view with validation errors
            return View(model);
        }

        // Get the user's ID from session data
        int Id = int.Parse(userIdStr);

        // Query the database to find the user profile
        var userProfile = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);

        if (userProfile == null)
        {
            // Handle the case where the user profile is not found
            return NotFound();
        }

        // Update the user profile with the new data
        userProfile.Username = model.Username;
        userProfile.Email = model.Email;
        
        // Hash the password before storing
        if (!String.IsNullOrEmpty(model.Password)) userProfile.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

        // Save changes to the database
        await _context.SaveChangesAsync();
        HttpContext.Session.SetString("Username", model.Username);
        HttpContext.Session.SetString("Email", model.Email);
        
       
        TempData["EditProfileSuccessMsg"] = "Profile updated successfully";


        HttpContext.Session.SetString("UserName",model.Username);
        return Redirect("/user/editprofile");
    }

    public IActionResult ForgotPassword()
    {

        return View("PasswordEmailForm");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        // Validate email (you may also want to check if the email exists in your database)
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError("Email", "Email is required");
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if(user == null)
        {
            // Return success message even if email is not found to prevent enumeration
            TempData["MailSuccess"] = "If the email is registered, a reset link has been sent successfully";
            return Redirect("/user/forgotpassword");
        }

        string resetToken = Guid.NewGuid().ToString();

        // Store the reset token and expiry in the database
        user.passwordResetToken = resetToken;
        user.PasswordResetExpiry = DateTime.Now.AddHours(24);
        await _context.SaveChangesAsync();

        var resetUrl = Url.Action("ResetPassword", "User", new { email = email, token = resetToken }, Request.Scheme);

        // Construct the email message
        var subject = "Password Reset";
        var message = $"Please click the following link to reset your password: {resetUrl}";

        // Send the email
        await SendEmailAsync(email, subject, message);
        TempData["MailSuccess"] = "Password reset link sent successfully";

        // Redirect to a page indicating that an email with password reset instructions has been sent
        return Redirect("/user/forgotPassword");
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        // Validate email and token
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            // Handle invalid or missing email/token
            return Redirect("/user/login");
           
        }

       

        var model = new PasswordResetModel { Email = email, token = token };
        return View("PasswordResetForm",model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(PasswordResetModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Retrieve the user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
        {
            
            return Redirect("/user/login");
        }

        // Validate the password reset token and expiry
        if (string.IsNullOrEmpty(user.passwordResetToken) ||
            user.passwordResetToken != model.token ||
            user.PasswordResetExpiry == null ||
            user.PasswordResetExpiry < DateTime.Now)
        {
            TempData["LoginFailureMessage"] = "Invalid or expired password reset link.";
            return Redirect("/user/login");
        }

        // Hash and update the user's password in the database
        user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

        // Clear the reset token after use
        user.passwordResetToken = null;

        TempData["myresetSuccess"] = "Your password changed successfully";
       
        // Save changes to the database
        await _context.SaveChangesAsync();

        
        return Redirect("/user/login");
    }




    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var smtpOptions = _smtpOptions; // No need for .Value as you're already accessing the SmtpOptions object

        var smtpClient = new SmtpClient(smtpOptions.ServerAddress, smtpOptions.ServerPort)
        {
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtpOptions.Username, smtpOptions.Password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpOptions.Username),
            Subject = subject,
            Body = message,
            IsBodyHtml = true
        };

        mailMessage.To.Add(email);

        await smtpClient.SendMailAsync(mailMessage);
        
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteProfile()
    {
        try
        {
            // Get the user ID from the session
            var userId = HttpContext.Session.GetString("UserId");

            // Check if the user ID is valid
            if (string.IsNullOrEmpty(userId))
            {
                // If the user ID is not found in the session, redirect to login page
                return RedirectToAction("Login");
            }

            // Find the user profile based on the user ID
            var userProfile = await _context.Users
                .Include(u => u.Comments)
                .Include(u => u.Blogs)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            // Check if the user profile exists
            if (userProfile == null)
            {
                // If the user profile is not found, return a not found response
                return NotFound();
            }

            // Remove related ranking entries that reference this user
            int parsedUserId = int.Parse(userId);
            var userCommentIds = userProfile.Comments.Select(c => c.Id).ToList();
            var userBlogIds = userProfile.Blogs.Select(b => b.Id).ToList();
            var blogCommentIds = await _context.Comments
                .Where(c => userBlogIds.Contains(c.BlogId))
                .Select(c => c.Id)
                .ToListAsync();
            var rankingCommentIds = userCommentIds.Concat(blogCommentIds).Distinct().ToList();

            var userRankings = _context.Ranking.Where(r =>
                r.UserId == parsedUserId ||
                (r.Type == "blog" && userBlogIds.Contains(r.TypeId)) ||
                ((r.Type == "comment" || r.Type == "comments") && rankingCommentIds.Contains(r.TypeId)));
            _context.Ranking.RemoveRange(userRankings);

            var relatedAlerts = _context.Alert.Where(a => userBlogIds.Contains(a.BlogPostId));
            _context.Alert.RemoveRange(relatedAlerts);

            var userComments = _context.Comments.Where(c => c.UserId == parsedUserId);
            _context.Comments.RemoveRange(userComments);

            // Remove the user profile from the database context.
            _context.Users.Remove(userProfile);

            // Save the changes to the database
            await _context.SaveChangesAsync();

            // Clear session data and sign out
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("SessionAuth");

            // Redirect to the home page or login page
            return RedirectToAction("Index", "Blog");
        }
        catch (Exception ex)
        {
            // Handle exceptions and return an error response
            return StatusCode(500, $"An error occurred while deleting the user profile: {ex.Message}");
        }
    }

}

