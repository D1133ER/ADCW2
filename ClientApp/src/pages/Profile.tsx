import { useState } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { useQuery } from "@tanstack/react-query";
import { auth as authApi, blogs as blogsApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { format } from "date-fns";
import { Calendar, Edit2, Check, X, PenSquare, BookOpen, FileText } from "lucide-react";

export default function Profile() {
  const { user, profile, refreshProfile } = useAuth();
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [displayName, setDisplayName] = useState(profile?.displayName ?? "");
  const [bio, setBio] = useState(profile?.bio ?? "");
  const [loading, setLoading] = useState(false);

  const { data: myBlogs } = useQuery({
    queryKey: ["my-blogs", user?.id],
    queryFn: () => blogsApi.myBlogs(),
    enabled: !!user,
  });

  if (!user) { navigate("/login"); return null; }

  const handleSave = async () => {
    setLoading(true);
    try {
      await authApi.updateProfile(displayName.trim(), bio.trim());
      toast.success("Profile updated!");
      setEditing(false);
      refreshProfile();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to update profile");
    } finally {
      setLoading(false);
    }
  };

  const publishedCount = myBlogs?.filter((b) => b.published).length ?? 0;
  const draftCount = myBlogs?.filter((b) => !b.published).length ?? 0;
  const initials = profile?.displayName?.slice(0, 2).toUpperCase() ?? "?";

  return (
    <div className="container max-w-3xl py-10 animate-fade-in">
      {/* Profile card */}
      <Card className="mb-8 overflow-hidden">
        <div className="h-24 bg-gradient-to-r from-navy via-primary/60 to-accent/40" />
        <CardContent className="pt-0 px-6 pb-6">
          <div className="flex flex-col sm:flex-row items-start sm:items-end gap-4 -mt-10">
            <Avatar className="h-20 w-20 ring-4 ring-card shadow-lg shrink-0">
              <AvatarImage src={profile?.avatarUrl ?? undefined} />
              <AvatarFallback className="bg-navy text-navy-foreground text-2xl font-heading font-bold">
                {initials}
              </AvatarFallback>
            </Avatar>
            <div className="flex-1 pt-2 sm:pt-0 min-w-0">
              {editing ? (
                <div className="space-y-3 mt-2">
                  <div>
                    <Label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Display name</Label>
                    <Input
                      value={displayName}
                      onChange={(e) => setDisplayName(e.target.value)}
                      className="h-9 mt-1"
                      placeholder="Your name"
                    />
                  </div>
                  <div>
                    <Label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Bio</Label>
                    <Textarea
                      value={bio}
                      onChange={(e) => setBio(e.target.value)}
                      className="min-h-[72px] mt-1 resize-none"
                      placeholder="Tell others about yourself..."
                    />
                  </div>
                  <div className="flex gap-2">
                    <Button size="sm" onClick={handleSave} disabled={loading} className="gap-1.5">
                      <Check className="h-3.5 w-3.5" /> {loading ? "Saving..." : "Save"}
                    </Button>
                    <Button size="sm" variant="ghost" onClick={() => setEditing(false)} className="gap-1.5">
                      <X className="h-3.5 w-3.5" /> Cancel
                    </Button>
                  </div>
                </div>
              ) : (
                <div>
                  <div className="flex items-center gap-3 flex-wrap">
                    <h1 className="text-2xl font-heading font-bold">{profile?.displayName}</h1>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-7 px-2.5 gap-1 text-xs text-muted-foreground hover:text-foreground"
                      onClick={() => {
                        setDisplayName(profile?.displayName ?? "");
                        setBio(profile?.bio ?? "");
                        setEditing(true);
                      }}
                    >
                      <Edit2 className="h-3.5 w-3.5" /> Edit
                    </Button>
                  </div>
                  {profile?.bio && (
                    <p className="text-muted-foreground text-sm mt-1.5 leading-relaxed">{profile.bio}</p>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* Stats row */}
          {!editing && (
            <div className="flex items-center gap-6 mt-6 pt-5 border-t border-border/60">
              <div className="flex items-center gap-2 text-sm">
                <BookOpen className="h-4 w-4 text-primary" />
                <span className="font-semibold">{publishedCount}</span>
                <span className="text-muted-foreground">Published</span>
              </div>
              {draftCount > 0 && (
                <div className="flex items-center gap-2 text-sm">
                  <FileText className="h-4 w-4 text-muted-foreground" />
                  <span className="font-semibold">{draftCount}</span>
                  <span className="text-muted-foreground">Draft{draftCount !== 1 ? "s" : ""}</span>
                </div>
              )}
              <Button
                size="sm"
                className="ml-auto gap-1.5 hover:-translate-y-px transition-transform duration-150"
                onClick={() => navigate("/create")}
              >
                <PenSquare className="h-4 w-4" /> New post
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Posts list */}
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xl font-heading font-semibold">My Posts</h2>
        {myBlogs && myBlogs.length > 0 && (
          <Badge variant="secondary" className="text-xs">{myBlogs.length} total</Badge>
        )}
      </div>

      <div className="space-y-3">
        {myBlogs?.map((blog, i) => (
          <Link key={blog.id} to={`/blog/${blog.id}`} className="block group animate-slide-up" style={{ animationDelay: `${i * 50}ms` }}>
            <Card className="border-border/60 hover:border-primary/30 hover:-translate-y-0.5 hover:shadow-md transition-all duration-200">
              <CardContent className="p-4 flex items-center justify-between gap-3">
                <div className="min-w-0 flex-1">
                  <h3 className="font-medium group-hover:text-primary transition-colors truncate">{blog.title}</h3>
                  <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      {format(new Date(blog.createdAt), "MMM d, yyyy")}
                    </span>
                  </div>
                </div>
                {!blog.published && (
                  <Badge variant="secondary" className="text-xs shrink-0">Draft</Badge>
                )}
              </CardContent>
            </Card>
          </Link>
        ))}
        {!myBlogs?.length && (
          <div className="text-center py-16 text-muted-foreground border border-dashed border-border rounded-xl">
            <PenSquare className="h-10 w-10 mx-auto mb-3 opacity-30" />
            <p className="mb-3">No posts yet.</p>
            <Button variant="outline" onClick={() => navigate("/create")} size="sm">
              Write your first post
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
