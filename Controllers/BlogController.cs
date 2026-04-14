using System;
using System.IO;
using System.Threading.Tasks;
using WeblogApplication.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using WeblogApplication.Data;
using System.Collections.Generic;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.EntityFrameworkCore;
using WeblogApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;


public class BlogController : Controller
{
    private readonly WeblogApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IBlogService _blogService;
    private readonly IRankingService _rankingService;
    private readonly ICommentService _commentService;

    public BlogController(
        WeblogApplicationDbContext context,
        IWebHostEnvironment hostingEnvironment,
        IBlogService blogService,
        IRankingService rankingService,
        ICommentService commentService)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
        _blogService = blogService;
        _rankingService = rankingService;
        _commentService = commentService;
    }


    public async Task<IActionResult> Index(string sortOrder = "random",int page = 1, int pageSize = 10)
    {
        ViewBag.SortOrder = sortOrder;

        var totalItems = await _blogService.GetTotalBlogCountAsync();
        if (totalItems == 0)
        {
            IEnumerable<BlogMetaData> data = new List<BlogMetaData>();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = 0;
            return View(data);
        }

        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        var currentPagePosts = await _blogService.GetBlogsWithMetadataAsync(sortOrder, page, pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SortOrder = sortOrder;

        return View(currentPagePosts);
    }

    [Authorize]
    public async Task<IActionResult> ManageBlogs()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return Redirect("/user/login");
        var blogs = await _blogService.GetBlogsByUserIdAsync(int.Parse(userIdStr));
        return View(blogs);
    }
    public IActionResult GetBlog(int id)
    {
        var blog = _context.Blogs.FirstOrDefault(b => b.Id == id);
        if (blog == null)
        {
            return NotFound();
        }

        return Json(blog);
    }
    // POST: /Blog/Create
    [HttpPost]
    public async Task<IActionResult> Create(BlogModel model, IFormFile image)
    {
        // Server-side authentication check
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Login", "User");
        }

        ModelState.Remove("ImagePath");
        ModelState.Remove("User");
        ModelState.Remove("Comments");
        if (ModelState.IsValid)
        {
      
           
            

            var compressedImage = await TinyImageAsync(image);
            var imagePath = await AddImageAsync(compressedImage);
            
            

            var blogPost = new BlogModel
            {
                Title = model.Title,
                ShortDescription = model.ShortDescription,
                Description = model.Description,
                CreatedBy = model.CreatedBy,
                ImagePath = imagePath,
                UserId = int.Parse(userIdStr),
                CreatedAt = DateTime.Now,
                Published = true,
            };

            _context.Blogs.Add(blogPost);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); // Redirect to blog listing page
        }
        return View("ManageBlogs");
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BlogModel model, IFormFile image)
    {
        // Server-side authentication check
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Login", "User");
        }

        try
        {

            var existingBlogPost = await _context.Blogs.FindAsync(id); // Find the existing blog post by ID
            if (existingBlogPost == null)
            {
                return NotFound(); // Return not found if the blog post with the given ID doesn't exist
            }

            // Handle file upload if a new image is provided
            if (image != null && image.Length > 0)
            {
            var compressedImage = await TinyImageAsync(image);
            existingBlogPost.ImagePath = await AddImageAsync(compressedImage);
        }

            // Update other properties
            existingBlogPost.Title = model.Title;
            existingBlogPost.ShortDescription = model.ShortDescription;
            existingBlogPost.Description = model.Description;
            
            existingBlogPost.LastUpdatedAt = DateTime.Now;

            _context.Blogs.Update(existingBlogPost); // Update the existing blog post entity
            await _context.SaveChangesAsync();

            

        return RedirectToAction("ManageBlogs"); // Return to the index view if model state is not valid
    }catch(Exception e)
        {
            return StatusCode(500, $"An error occurred while editing the blog post: {e.Message}");
        }
        }



    // Method to handle file upload
    private async Task<string> AddImageAsync(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
        var uniqueFileName = Guid.NewGuid().ToString() + "_image.jpg"; // Change the extension if needed
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        await System.IO.File.WriteAllBytesAsync(filePath, imageData);

        return "/uploads/" + uniqueFileName; // Return the relative path to the uploaded image
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        // Server-side authentication check
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Login", "User");
        }

        var blogPost = await _context.Blogs.FindAsync(id);
        if (blogPost == null)
        {
            return NotFound();
        }

        var commentIds = await _context.Comments
            .Where(c => c.BlogId == id)
            .Select(c => c.Id)
            .ToListAsync();

        var relatedRankings = await _context.Ranking
            .Where(r =>
                (r.Type == "blog" && r.TypeId == id) ||
                ((r.Type == "comment" || r.Type == "comments") && commentIds.Contains(r.TypeId)))
            .ToListAsync();

        var relatedAlerts = await _context.Alert
            .Where(a => a.BlogPostId == id)
            .ToListAsync();

        _context.Ranking.RemoveRange(relatedRankings);
        _context.Alert.RemoveRange(relatedAlerts);

        _context.Blogs.Remove(blogPost);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index)); // Redirect to the blog listing page
    }
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ModifyRankCount(int postId, string action, string type)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return Unauthorized("Please login to continue.");
        }

        int userId = int.Parse(userIdStr);
        var (newLike, newDislike) = await _rankingService.ModifyRankAsync(postId, action, type, userId);
        return Json(new { newLike, newDislike });
    }
    public IActionResult BlogComments(int blogId)
    {
        // Retrieve all comments for the specified blog from the database
        var comments = _context.Comments
            .Where(c => c.BlogId == blogId)
            .ToList();
        var commentsCounts = _context.Ranking.Where(c => c.Type == "comments" && c.TypeId == blogId);
        

        // Pass the comments to the view or perform further processing
        return Json(new {comments,commentsCounts});
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PostComment(int postId, string commentText, string CreatedBy)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return Unauthorized("Please login to comment.");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                return BadRequest("Comment text is required.");
            }

            int userId = int.Parse(userIdStr);
            string username = HttpContext.Session.GetString("Username") ?? "";
            var comment = await _commentService.PostCommentAsync(postId, commentText, userId, username);

            return Ok(new { id = comment.Id, message = comment.Text, createdDate = comment.CreatedDate });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EditComment(int commentId, string editedText)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return Unauthorized("Please login to edit comments.");
        }

        try
        {
            int userId = int.Parse(userIdStr);
            var success = await _commentService.EditCommentAsync(commentId, editedText, userId);
            if (!success)
                return NotFound();

            return Ok(new { message = "Comment updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DeleteComment(string commentId)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return Unauthorized("Please login to delete comments.");
        }

        int parsedId = int.Parse(commentId);
        int userId = int.Parse(userIdStr);
        var success = await _commentService.DeleteCommentAsync(parsedId, userId);
        if (!success)
            return NotFound();

        return Ok(new { success = "Deleted" });
    }

    private async Task<byte[]> TinyImageAsync(IFormFile imageFile)
    {
        using (var inputStream = imageFile.OpenReadStream())
        {
            using (var outputStream = new MemoryStream())
            {
                using (var image = Image.Load(inputStream))
                {
                    var quality = 75; // Initial quality setting

                    // Save the image with the initial quality setting
                    image.Save(outputStream, new JpegEncoder
                    {
                        Quality = quality
                    });

                    // Check the size of the compressed image
                    while (outputStream.Length > 3 * 1024 * 1024) // Check if size exceeds 3 MB
                    {
                        outputStream.SetLength(0); // Reset the output stream
                        quality -= 5; // Reduce quality by 5

                        // Save the image with the adjusted quality
                        image.Save(outputStream, new JpegEncoder
                        {
                            Quality = quality
                        });

                        outputStream.Seek(0, SeekOrigin.Begin); // Reset stream position for next iteration
                    }

                    return outputStream.ToArray();
                }
            }
        }

    }

   

        public IActionResult GetUnreadAlertForUser(Boolean noread)
        {
        // Fetch unread notifications for the specified user, ordered by CreatedAt in descending order


        string userIdString = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            // Handle the case where session user ID is not available or not in the correct format
            return BadRequest("Session user ID is missing or invalid.");
        }

        // Fetch unread notifications for the current user
        var unreadAlert = _context.Blogs
            .Join(_context.Alert,
                blog => blog.Id,
                notification => notification.BlogPostId,
                (blog, notification) => new { Blog = blog, Alert = notification })
            .Where(joinResult => joinResult.Blog.UserId == userId )
            .Select(joinResult => new
            {
                Id = joinResult.Alert.Id,
                BlogTitle = joinResult.Blog.Title,
                Message = joinResult.Alert.Message,
                isRead = joinResult.Alert.isRead
            })
            .OrderBy(joinResult => joinResult.isRead)
            .ToList();

        // Get the IDs of the unread notifications
        var notificationIds = unreadAlert.Select(notification => notification.Id).ToList();

        // Update the isRead status of fetched notifications to true
        var notificationsToUpdate = _context.Alert
            .Where(notification => notificationIds.Contains(notification.Id))
            .ToList();

        if(!noread)
            notificationsToUpdate.ForEach(notification => notification.isRead = true);

        // Save changes to the database
        _context.SaveChanges();

        return Json(unreadAlert);

    }
    [HttpPost]
    public IActionResult CreateAlert(int blogPostId, string message)
    {
        // Server-side authentication check
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return Unauthorized("Please login to create alerts.");
        }

        try
        {
            // Create a new notification object
            var notification = new AlertModel
            {
                BlogPostId = blogPostId,
                Message = message,
                CreatedAt = DateTime.Now, // You can also use DateTimeOffset.UtcNow for UTC time
                isRead = false // By default, the notification is unread
            };

            // Add the notification to the database context
            _context.Alert.Add(notification);

            // Save changes to the database
            _context.SaveChanges();

            return Ok("Alert Created successfully.");
        }
        catch (Exception ex)
        {
            // Handle exceptions
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

}
