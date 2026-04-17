namespace WeblogApplication.Models
{
    public class SignUpRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class EditProfileRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
    }

    public class CreateBlogRequest
    {
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
    }

    public class EditBlogRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
    }
}
