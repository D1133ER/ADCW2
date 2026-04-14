import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/contexts/AuthContext";
import { blogs as blogsApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Skeleton } from "@/components/ui/skeleton";
import { toast } from "sonner";
import { Save, PenSquare } from "lucide-react";

export default function EditPost() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [excerpt, setExcerpt] = useState("");
  const [published, setPublished] = useState(true);
  const [loading, setLoading] = useState(false);

  const { data: blog, isLoading } = useQuery({
    queryKey: ["blog", id],
    queryFn: () => blogsApi.get(id!),
    enabled: !!id,
  });

  useEffect(() => {
    if (blog) {
      setTitle(blog.title);
      setContent(blog.content);
      setExcerpt(blog.excerpt ?? "");
      setPublished(blog.published ?? true);
    }
  }, [blog]);

  if (!user) { navigate("/login"); return null; }
  if (isLoading) return <div className="container max-w-3xl py-10"><Skeleton className="h-[500px]" /></div>;
  if (blog && blog.userId !== user.id) { navigate("/"); return null; }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await blogsApi.update(id!, { title: title.trim(), content: content.trim(), excerpt: excerpt.trim() || undefined, published });
      toast.success("Post updated!");
      navigate(`/blog/${id}`);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to update post");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container max-w-3xl py-10 animate-fade-in">
      {/* Page header */}
      <div className="flex items-center gap-3 mb-8">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <PenSquare className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-heading font-bold">Edit post</h1>
          <p className="text-sm text-muted-foreground line-clamp-1">{title || "Untitled post"}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Title */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="title">Title <span className="text-destructive">*</span></Label>
            <span className="text-xs text-muted-foreground">{title.length}/100</span>
          </div>
          <Input
            id="title"
            value={title}
            onChange={(e) => setTitle(e.target.value.slice(0, 100))}
            placeholder="Your post title"
            className="text-lg font-medium h-12"
            required
          />
        </div>

        {/* Excerpt */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="excerpt">Excerpt</Label>
            <span className="text-xs text-muted-foreground">{excerpt.length}/200</span>
          </div>
          <Input
            id="excerpt"
            value={excerpt}
            onChange={(e) => setExcerpt(e.target.value.slice(0, 200))}
            placeholder="A short summary shown in post cards (optional)"
          />
        </div>

        {/* Content */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="content">Content <span className="text-destructive">*</span></Label>
            <span className="text-xs text-muted-foreground">{content.split(/\s+/).filter(Boolean).length} words</span>
          </div>
          <Textarea
            id="content"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Write your story..."
            className="min-h-[320px] resize-y font-body leading-relaxed"
            required
          />
        </div>

        {/* Footer: publish toggle + submit */}
        <div className="flex items-center justify-between rounded-xl border border-border px-5 py-4">
          <div className="flex items-center gap-3">
            <Switch checked={published} onCheckedChange={setPublished} id="published" />
            <div>
              <Label htmlFor="published" className="cursor-pointer font-medium">
                {published ? "Published" : "Draft"}
              </Label>
              <p className="text-xs text-muted-foreground">
                {published ? "Visible to everyone" : "Only visible to you"}
              </p>
            </div>
          </div>
          <Button type="submit" disabled={loading} className="gap-2 min-w-[130px]">
            {loading ? (
              <><span className="h-4 w-4 rounded-full border-2 border-primary-foreground/30 border-t-primary-foreground animate-spin" /> Saving...</>
            ) : (
              <><Save className="h-4 w-4" /> Save changes</>
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}
