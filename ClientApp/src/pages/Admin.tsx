import { useAuth } from "@/contexts/AuthContext";
import { useQuery } from "@tanstack/react-query";
import { admin as adminApi } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { useNavigate, Link } from "react-router-dom";
import { Users, FileText, MessageSquare, Shield, TrendingUp } from "lucide-react";
import { format } from "date-fns";

export default function Admin() {
  const { user, isAdmin } = useAuth();
  const navigate = useNavigate();

  if (!user || !isAdmin) { navigate("/"); return null; }

  const { data: stats } = useQuery({
    queryKey: ["admin-stats"],
    queryFn: () => adminApi.stats(),
  });

  const { data: recentUsers } = useQuery({
    queryKey: ["admin-recent-users"],
    queryFn: () => adminApi.recentUsers(10),
  });

  const { data: recentBlogs } = useQuery({
    queryKey: ["admin-recent-blogs"],
    queryFn: () => adminApi.recentBlogs(10),
  });

  const statCards = [
    {
      label: "Total Users",
      value: stats?.users ?? 0,
      icon: Users,
      color: "text-primary",
      bg: "bg-primary/10",
    },
    {
      label: "Total Posts",
      value: stats?.blogs ?? 0,
      icon: FileText,
      color: "text-accent",
      bg: "bg-accent/10",
    },
    {
      label: "Total Comments",
      value: stats?.comments ?? 0,
      icon: MessageSquare,
      color: "text-navy",
      bg: "bg-navy/10",
    },
  ];

  return (
    <div className="container py-10 animate-fade-in">
      {/* Header */}
      <div className="flex items-center gap-3 mb-10">
        <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <Shield className="h-6 w-6" />
        </div>
        <div>
          <h1 className="text-3xl font-heading font-bold">Admin Dashboard</h1>
          <p className="text-sm text-muted-foreground">Platform overview & management</p>
        </div>
      </div>

      {/* Stats */}
      <div className="grid md:grid-cols-3 gap-5 mb-10">
        {statCards.map(({ label, value, icon: Icon, color, bg }, i) => (
          <Card
            key={label}
            className="border-border/60 hover:-translate-y-0.5 hover:shadow-lg transition-all duration-200 animate-slide-up"
            style={{ animationDelay: `${i * 80}ms` }}
          >
            <CardContent className="p-6">
              <div className="flex items-center justify-between mb-4">
                <p className="text-sm font-medium text-muted-foreground">{label}</p>
                <div className={`flex h-9 w-9 items-center justify-center rounded-lg ${bg}`}>
                  <Icon className={`h-5 w-5 ${color}`} />
                </div>
              </div>
              <p className="text-4xl font-heading font-bold">{value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Tables */}
      <div className="grid lg:grid-cols-2 gap-8">
        <Card className="animate-slide-up delay-200">
          <CardHeader className="pb-3">
            <CardTitle className="text-lg flex items-center gap-2">
              <Users className="h-5 w-5 text-primary" /> Recent Users
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {recentUsers?.map((u) => (
                <div key={u.id} className="flex items-center gap-3 py-1.5 border-b border-border/40 last:border-0">
                  <Avatar className="h-8 w-8 shrink-0">
                    <AvatarImage src={u.avatarUrl ?? undefined} />
                    <AvatarFallback className="text-[10px] bg-muted font-medium">
                      {u.displayName?.slice(0, 2).toUpperCase() ?? "?"}
                    </AvatarFallback>
                  </Avatar>
                  <span className="font-medium text-sm flex-1 truncate">{u.displayName ?? "—"}</span>
                  <span className="text-muted-foreground text-xs shrink-0">{format(new Date(u.createdAt), "MMM d, yyyy")}</span>
                </div>
              ))}
              {!recentUsers?.length && (
                <p className="text-muted-foreground text-sm text-center py-4">No users yet.</p>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="animate-slide-up delay-300">
          <CardHeader className="pb-3">
            <CardTitle className="text-lg flex items-center gap-2">
              <FileText className="h-5 w-5 text-accent" /> Recent Posts
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {recentBlogs?.map((b) => (
                <div key={b.id} className="flex items-start gap-3 py-1.5 border-b border-border/40 last:border-0">
                  <div className="flex-1 min-w-0">
                    <Link
                      to={`/blog/${b.id}`}
                      className="font-medium text-sm hover:text-primary transition-colors line-clamp-1"
                    >
                      {b.title}
                    </Link>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      by {b.profiles?.displayName ?? "—"}
                      {!b.published && (
                        <span className="ml-2 px-1.5 py-0.5 rounded text-[10px] bg-muted font-medium">draft</span>
                      )}
                    </p>
                  </div>
                  <span className="text-muted-foreground text-xs shrink-0">{format(new Date(b.createdAt), "MMM d")}</span>
                </div>
              ))}
              {!recentBlogs?.length && (
                <p className="text-muted-foreground text-sm text-center py-4">No posts yet.</p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
