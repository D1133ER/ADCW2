using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WeblogApplication.Models;

namespace WeblogApplication.Data
{
    public class WeblogApplicationDbContext : DbContext
    {
        public WeblogApplicationDbContext(DbContextOptions<WeblogApplicationDbContext> options) : base(options)
        {
        }

        // DbSet properties for other entities
        public DbSet<BlogModel> Blogs { get; set; }
        public  DbSet<UserModel> Users { get; set; }
        public DbSet<CommentModel> Comments { get; set; }
        public DbSet<AlertModel> Alert { get; set; }
        public DbSet<RankingModel> Ranking { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserModel>()
                .HasMany(u => u.Blogs)
                .WithOne(b => b.User)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentModel>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommentModel>()
                .HasOne(c => c.Blog)
                .WithMany(b => b.Comments)
                .HasForeignKey(c => c.BlogId)
                .OnDelete(DeleteBehavior.Cascade); 
            
           
        }

    }
}
