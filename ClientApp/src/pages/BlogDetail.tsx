import { useParams, useNavigate, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { blogs as blogsApi, comments as commentsApi } from "@/lib/api";
import { useAuth } from "@/contexts/AuthContext";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { toast } from "sonner";
import { format } from "date-fns";
import { ArrowLeft, Edit, Trash2, ThumbsUp, ThumbsDown, MessageSquare, Calendar, Clock, User as UserIcon } from "lucide-react";
import { useState } from "react";

function readingTime(content: string): string {
  const words = content?.trim().split(/\s+/).length ?? 0;
  const mins = Math.max(1, Math.round(words / 200));
  return `${mins} min read`;
}

export default function BlogDetail() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [comment, setComment] = useState("");

  const { data: blog, isLoading } = useQuery({
    queryKey: ["blog", id],
    queryFn: () => blogsApi.get(id!),
    enabled: !!id,
  });

  const { data: comments } = useQuery({
    queryKey: ["comments", id],
    queryFn: () => blogsApi.getComments(id!),
    enabled: !!id,
  });

  const addComment = useMutation({
    mutationFn: async () => {
      if (!user || !comment.trim()) return;
      await commentsApi.add(id!, comment.trim());
    },
    onSuccess: () => {
      setComment("");
      qc.invalidateQueries({ queryKey: ["comments", id] });
      toast.success("Comment added!");
    },
    onError: () => toast.error("Failed to add comment"),
  });

  const deleteBlog = useMutation({
    mutationFn: () => blogsApi.delete(id!),
    onSuccess: () => {
      toast.success("Post deleted");
      navigate("/");
    },
  });

  const vote = useMutation({
    mutationFn: async ({ commentId, voteType }: { commentId: string; voteType: 1 | -1 }) => {
      if (!user) return;
      await commentsApi.vote(commentId, voteType);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["comments", id] }),
  });

  if (isLoading) {
    return (
      <div className="container max-w-3xl py-10">
        <Skeleton className="h-8 w-24 mb-8" />
        <Skeleton className="h-[300px] w-full rounded-2xl mb-8" />
        <Skeleton className="h-10 w-3/4 mb-4" />
        <Skeleton className="h-5 w-1/2 mb-8" />
        <div className="space-y-3">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-4/5" />
        </div>
      </div>
    );
  }

  if (!blog) {
    return (
      <div className="container py-20 text-center animate-fade-in">
        <h1 className="text-2xl font-heading font-bold mb-3">Post not found</h1>
        <p className="text-muted-foreground mb-6">This post may have been deleted or doesn't exist.</p>
        <Button onClick={() => navigate("/")} variant="outline">Go home</Button>
      </div>
    );
  }

  const isAuthor = user?.id === blog.userId;

  return (
    <article className="container max-w-3xl py-10 animate-fade-in">
      <Button
        variant="ghost"
        size="sm"
        onClick={() => navigate(-1)}
        className="mb-8 gap-1.5 hover:gap-2.5 transition-all duration-200 -ml-2"
      >
        <ArrowLeft className="h-4 w-4" /> Back
      </Button>

      {blog.coverImageUrl && (
        <div className="rounded-2xl overflow-hidden mb-10 aspect-video shadow-xl shadow-foreground/5">
          <img
            src={blog.coverImageUrl}
            alt={blog.title}
            className="w-full h-full object-cover"
          />
        </div>
      )}

      {/* Author meta */}
      <div className="flex items-center gap-4 mb-6 text-sm text-muted-foreground">
        <div className="flex items-center gap-2">
          <Avatar className="h-9 w-9 ring-2 ring-border">
            <AvatarImage src={blog.profiles?.avatarUrl ?? undefined} />
            <AvatarFallback className="text-xs bg-primary text-primary-foreground">
              {blog.profiles?.displayName?.slice(0, 2).toUpperCase() ?? "?"}
            </AvatarFallback>
          </Avatar>
          <div>
            <p className="font-medium text-foreground text-sm">{blog.profiles?.displayName ?? "Anonymous"}</p>
          </div>
        </div>
        <span className="text-border">|</span>
        <span className="flex items-center gap-1.5">
          <Calendar className="h-3.5 w-3.5" />
          {format(new Date(blog.createdAt), "MMM d, yyyy")}
        </span>
        <span className="flex items-center gap-1.5">
          <Clock className="h-3.5 w-3.5" />
          {readingTime(blog.content)}
        </span>
      </div>

      <h1 className="text-3xl md:text-4xl font-heading font-bold mb-8 leading-tight">{blog.title}</h1>

      {blog.excerpt && (
        <p className="text-lg text-muted-foreground border-l-4 border-primary/30 pl-4 mb-8 italic leading-relaxed">
          {blog.excerpt}
        </p>
      )}

      <div className="prose prose-slate max-w-none mb-12 whitespace-pre-wrap text-foreground/85 leading-[1.85] text-[1.05rem]">
        {blog.content}
      </div>

      {isAuthor && (
        <div className="flex gap-2 mb-12 pt-6 border-t border-border/60">
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate(`/edit/${blog.id}`)}
            className="gap-1.5 hover:-translate-y-px transition-transform duration-150"
          >
            <Edit className="h-4 w-4" /> Edit post
          </Button>
          <Button
            variant="destructive"
            size="sm"
            onClick={() => {
              if (window.confirm("Delete this post? This cannot be undone.")) {
                deleteBlog.mutate();
              }
            }}
            disabled={deleteBlog.isPending}
            className="gap-1.5"
          >
            <Trash2 className="h-4 w-4" /> {deleteBlog.isPending ? "Deleting..." : "Delete"}
          </Button>
        </div>
      )}

      {/* Comments section */}
      <section className="border-t border-border/60 pt-10">
        <h2 className="text-xl font-heading font-semibold mb-8 flex items-center gap-2">
          <MessageSquare className="h-5 w-5 text-primary" />
          {comments?.length ? `${comments.length} Comment${comments.length !== 1 ? "s" : ""}` : "Comments"}
        </h2>

        {user ? (
          <div className="mb-10 space-y-3 bg-muted/40 rounded-xl p-4 border border-border/50">
            <div className="flex items-center gap-2 mb-2">
              <Avatar className="h-7 w-7">
                <AvatarImage src={undefined} />
                <AvatarFallback className="text-[10px] bg-primary text-primary-foreground">
                  {user.email?.slice(0, 2).toUpperCase()}
                </AvatarFallback>
              </Avatar>
              <span className="text-sm font-medium text-muted-foreground">Leave a comment</span>
            </div>
            <Textarea
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Share your thoughts..."
              className="min-h-[100px] bg-card resize-none focus:shadow-sm transition-shadow"
            />
            <div className="flex justify-end">
              <Button
                onClick={() => addComment.mutate()}
                disabled={!comment.trim() || addComment.isPending}
                size="sm"
                className="gap-1.5"
              >
                {addComment.isPending ? (
                  <span className="flex items-center gap-2">
                    <span className="h-3.5 w-3.5 rounded-full border-2 border-primary-foreground/30 border-t-primary-foreground animate-spin" />
                    Posting...
                  </span>
                ) : "Post comment"}
              </Button>
            </div>
          </div>
        ) : (
          <div className="mb-8 p-4 rounded-xl bg-muted/40 border border-border/50 text-center">
            <p className="text-muted-foreground text-sm">
              <Link to="/login" className="text-primary hover:underline font-medium">Sign in</Link>
              {" "}to leave a comment.
            </p>
          </div>
        )}

        <div className="space-y-4">
          {comments?.map((c, i) => {
            const upvotes = c.commentVotes?.filter((v) => v.voteType === 1).length ?? 0;
            const downvotes = c.commentVotes?.filter((v) => v.voteType === -1).length ?? 0;
            const userVote = c.commentVotes?.find((v) => v.userId === user?.id);

            return (
              <div
                key={c.id}
                className="rounded-xl border border-border/60 bg-card p-5 animate-slide-up hover:border-border transition-colors"
                style={{ animationDelay: `${i * 50}ms` }}
              >
                <div className="flex items-center gap-2.5 mb-3">
                  <Avatar className="h-7 w-7">
                    <AvatarImage src={c.profiles?.avatarUrl ?? undefined} />
                    <AvatarFallback className="text-[10px] bg-muted text-muted-foreground">
                      {c.profiles?.displayName?.slice(0, 2).toUpperCase() ?? "?"}
                    </AvatarFallback>
                  </Avatar>
                  <span className="text-sm font-semibold">{c.profiles?.displayName ?? "Anonymous"}</span>
                  <span className="text-xs text-muted-foreground ml-auto">{format(new Date(c.createdAt), "MMM d, yyyy")}</span>
                </div>
                <p className="text-sm text-foreground/85 mb-3 whitespace-pre-wrap leading-relaxed">{c.content}</p>
                {user && (
                  <div className="flex items-center gap-1 pt-2 border-t border-border/40">
                    <Button
                      variant="ghost"
                      size="sm"
                      className={`h-7 px-2.5 text-xs gap-1 rounded-full transition-all duration-150 ${
                        (userVote as any)?.voteType === 1
                          ? "bg-primary/10 text-primary font-semibold"
                          : "text-muted-foreground hover:text-foreground"
                      }`}
                      onClick={() => vote.mutate({ commentId: c.id, voteType: 1 })}
                    >
                      <ThumbsUp className="h-3.5 w-3.5" /> {upvotes > 0 && upvotes}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className={`h-7 px-2.5 text-xs gap-1 rounded-full transition-all duration-150 ${
                        (userVote as any)?.voteType === -1
                          ? "bg-destructive/10 text-destructive font-semibold"
                          : "text-muted-foreground hover:text-foreground"
                      }`}
                      onClick={() => vote.mutate({ commentId: c.id, voteType: -1 })}
                    >
                      <ThumbsDown className="h-3.5 w-3.5" /> {downvotes > 0 && downvotes}
                    </Button>
                  </div>
                )}
              </div>
            );
          })}
          {!comments?.length && (
            <div className="text-center py-10 text-muted-foreground">
              <MessageSquare className="h-10 w-10 mx-auto mb-3 opacity-30" />
              <p className="text-sm">No comments yet. Be the first to share your thoughts!</p>
            </div>
          )}
        </div>
      </section>
    </article>
  );
}
