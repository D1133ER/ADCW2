import { useQuery } from "@tanstack/react-query";
import { blogs as blogsApi } from "@/lib/api";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/contexts/AuthContext";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { format } from "date-fns";
import { Calendar, User, Clock, PenSquare, ArrowRight, TrendingUp } from "lucide-react";

function readingTime(content: string): string {
  const words = content?.trim().split(/\s+/).length ?? 0;
  const mins = Math.max(1, Math.round(words / 200));
  return `${mins} min read`;
}

export default function Index() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const { data: blogs, isLoading } = useQuery({
    queryKey: ["blogs"],
    queryFn: () => blogsApi.list(),
  });

  const featured = blogs?.find((b) => b.isFeatured) ?? blogs?.[0];
  const rest = blogs?.filter((b) => b.id !== featured?.id) ?? [];

  if (isLoading) {
    return (
      <div className="container py-10">
        <Skeleton className="h-[420px] w-full rounded-2xl mb-12" />
        <div className="flex items-center gap-3 mb-6">
          <Skeleton className="h-7 w-32" />
          <Skeleton className="h-5 w-20 rounded-full" />
        </div>
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="rounded-xl border bg-card overflow-hidden">
              <Skeleton className="h-44 w-full" />
              <div className="p-5 space-y-3">
                <Skeleton className="h-5 w-4/5" />
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-3/4" />
                <Skeleton className="h-3 w-1/2" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!blogs?.length) {
    return (
      <div className="container py-24 text-center animate-fade-in">
        <div className="mx-auto max-w-md">
          <div className="flex h-20 w-20 items-center justify-center rounded-2xl bg-primary/10 text-primary mx-auto mb-6 animate-float">
            <PenSquare className="h-9 w-9" />
          </div>
          <h1 className="text-4xl font-heading font-bold mb-4">Welcome to Weblog</h1>
          <p className="text-muted-foreground text-lg mb-8">
            No posts yet. Be the first to share your ideas with the world!
          </p>
          {user ? (
            <Button size="lg" onClick={() => navigate("/create")} className="gap-2 shadow-lg hover:shadow-xl hover:-translate-y-0.5 transition-all duration-200">
              <PenSquare className="h-5 w-5" /> Write your first post
            </Button>
          ) : (
            <div className="flex gap-3 justify-center">
              <Button size="lg" onClick={() => navigate("/signup")} className="shadow-lg">Get started</Button>
              <Button size="lg" variant="outline" onClick={() => navigate("/login")}>Log in</Button>
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="container py-10">
      {/* Featured post hero */}
      {featured && (
        <Link to={`/blog/${featured.id}`} className="group block mb-14 animate-fade-in">
          <div className="relative overflow-hidden rounded-2xl bg-navy text-navy-foreground min-h-[400px] flex flex-col justify-end">
            {featured.coverImageUrl ? (
              <img
                src={featured.coverImageUrl}
                alt={featured.title}
                className="absolute inset-0 w-full h-full object-cover opacity-25 group-hover:opacity-30 group-hover:scale-105 transition-all duration-700"
              />
            ) : (
              <div className="absolute inset-0 bg-gradient-to-br from-navy via-primary/30 to-accent/20" />
            )}
            {/* Gradient overlay */}
            <div className="absolute inset-0 bg-gradient-to-t from-navy/90 via-navy/40 to-transparent" />

            <div className="relative p-8 md:p-14">
              <div className="flex items-center gap-3 mb-5">
                <Badge className="bg-primary text-primary-foreground font-medium px-3 py-1">
                  <TrendingUp className="h-3 w-3 mr-1" /> Featured
                </Badge>
                <span className="text-navy-foreground/50 text-sm flex items-center gap-1">
                  <Clock className="h-3.5 w-3.5" />
                  {readingTime(featured.content)}
                </span>
              </div>
              <h1 className="text-3xl md:text-5xl font-heading font-bold mb-4 group-hover:text-teal-light transition-colors duration-300 leading-tight">
                {featured.title}
              </h1>
              {featured.excerpt && (
                <p className="text-navy-foreground/75 text-lg max-w-2xl mb-6 line-clamp-2">{featured.excerpt}</p>
              )}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4 text-sm text-navy-foreground/60">
                  <div className="flex items-center gap-2">
                    <Avatar className="h-6 w-6">
                      <AvatarImage src={featured.profiles?.avatarUrl ?? undefined} />
                      <AvatarFallback className="text-[10px] bg-primary/40 text-primary-foreground">
                        {featured.profiles?.displayName?.slice(0, 2).toUpperCase() ?? "?"}
                      </AvatarFallback>
                    </Avatar>
                    <span>{featured.profiles?.displayName ?? "Anonymous"}</span>
                  </div>
                  <span className="flex items-center gap-1">
                    <Calendar className="h-3.5 w-3.5" />
                    {format(new Date(featured.createdAt), "MMM d, yyyy")}
                  </span>
                </div>
                <span className="text-navy-foreground/50 text-sm flex items-center gap-1 group-hover:text-teal-light group-hover:translate-x-1 transition-all duration-200">
                  Read article <ArrowRight className="h-4 w-4" />
                </span>
              </div>
            </div>
          </div>
        </Link>
      )}

      {/* Grid header */}
      <div className="flex items-center justify-between mb-8 animate-slide-up delay-100">
        <div className="flex items-center gap-3">
          <h2 className="text-2xl font-heading font-bold">Latest Posts</h2>
          {rest.length > 0 && (
            <Badge variant="secondary" className="text-xs font-medium">
              {rest.length} {rest.length === 1 ? "post" : "posts"}
            </Badge>
          )}
        </div>
        {user && (
          <Button variant="outline" size="sm" onClick={() => navigate("/create")} className="gap-1.5 hover:-translate-y-px transition-transform duration-150">
            <PenSquare className="h-4 w-4" /> New post
          </Button>
        )}
      </div>

      {/* Post grid */}
      <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
        {rest.map((blog, i) => (
          <Link
            key={blog.id}
            to={`/blog/${blog.id}`}
            className="group animate-slide-up"
            style={{ animationDelay: `${150 + i * 60}ms` }}
          >
            <Card className="overflow-hidden h-full border-border/60 hover:border-primary/30 hover:-translate-y-1 hover:shadow-xl hover:shadow-foreground/5 transition-all duration-300">
              {blog.coverImageUrl ? (
                <div className="aspect-video overflow-hidden bg-muted">
                  <img
                    src={blog.coverImageUrl}
                    alt={blog.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                </div>
              ) : (
                <div className="aspect-video bg-gradient-to-br from-primary/10 via-accent/5 to-muted flex items-center justify-center">
                  <div className="text-4xl font-heading font-bold text-primary/20">
                    {blog.title.charAt(0).toUpperCase()}
                  </div>
                </div>
              )}
              <CardContent className="p-5 flex flex-col gap-3">
                <h3 className="font-heading font-semibold text-lg group-hover:text-primary transition-colors duration-200 line-clamp-2 leading-snug">
                  {blog.title}
                </h3>
                {blog.excerpt && (
                  <p className="text-muted-foreground text-sm line-clamp-2 leading-relaxed flex-1">{blog.excerpt}</p>
                )}
                <div className="flex items-center justify-between text-xs text-muted-foreground pt-1 border-t border-border/50">
                  <div className="flex items-center gap-2">
                    <Avatar className="h-5 w-5">
                      <AvatarImage src={blog.profiles?.avatarUrl ?? undefined} />
                      <AvatarFallback className="text-[8px] bg-muted">
                        {blog.profiles?.displayName?.slice(0, 2).toUpperCase() ?? "?"}
                      </AvatarFallback>
                    </Avatar>
                    <span className="font-medium text-foreground/70">{blog.profiles?.displayName ?? "Anonymous"}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {readingTime(blog.content)}
                    </span>
                    <span>·</span>
                    <span>{format(new Date(blog.createdAt), "MMM d")}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {/* Empty rest state */}
      {rest.length === 0 && featured && (
        <div className="text-center py-12 text-muted-foreground animate-fade-in">
          <p>More posts coming soon.</p>
        </div>
      )}
    </div>
  );
}
