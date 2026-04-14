using Microsoft.EntityFrameworkCore;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Implementation
{
    public class RankingService : IRankingService
    {
        private readonly WeblogApplicationDbContext _context;

        public RankingService(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(int Like, int Dislike)> ModifyRankAsync(int postId, string action, string type, int userId)
        {
            var existing = await _context.Ranking
                .FirstOrDefaultAsync(r => r.TypeId == postId && r.Type == type && r.UserId == userId);

            var actualPost = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == postId);

            if (existing != null)
            {
                if (action == "like")
                {
                    if (existing.Like != 0)
                    {
                        existing.Like = 0;
                        if (actualPost != null) actualPost.Popularity -= 2;
                    }
                    else
                    {
                        existing.Like = 1;
                        existing.Dislike = 0;
                        if (actualPost != null) actualPost.Popularity += 2;
                    }
                }
                else if (action == "dislike")
                {
                    if (existing.Dislike == 1)
                    {
                        existing.Dislike = 0;
                        if (actualPost != null) actualPost.Popularity += 1;
                    }
                    else
                    {
                        if (existing.Like != 0)
                        {
                            existing.Like = 0;
                        }
                        existing.Dislike = 1;
                        if (actualPost != null) actualPost.Popularity--;
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                var newRanking = new RankingModel
                {
                    TypeId = postId,
                    Type = type,
                    UserId = userId,
                    Like = action == "like" ? 1 : 0,
                    Dislike = action == "dislike" ? 1 : 0,
                };

                if (actualPost != null)
                {
                    actualPost.Popularity += action == "like" ? 2 : -2;
                }

                _context.Ranking.Add(newRanking);
                await _context.SaveChangesAsync();
            }

            var voteCounts = await _context.Ranking
                .Where(v => v.TypeId == postId && v.Type == type)
                .GroupBy(v => 1)
                .Select(g => new
                {
                    Like = g.Sum(v => v.Like),
                    Dislike = g.Sum(v => v.Dislike)
                })
                .FirstOrDefaultAsync();

            return (voteCounts?.Like ?? 0, voteCounts?.Dislike ?? 0);
        }

        public async Task<int> GetTotalLikesAsync()
        {
            return await _context.Ranking.Where(r => r.Type == "blog").SumAsync(r => r.Like);
        }

        public async Task<int> GetTotalDislikesAsync()
        {
            return await _context.Ranking.Where(r => r.Type == "blog").SumAsync(r => r.Dislike);
        }
    }
}
