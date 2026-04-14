import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/contexts/AuthContext";
import { blogs as blogsApi, uploadApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { toast } from "sonner";
import { ImagePlus, X, Send, FilePenLine } from "lucide-react";

export default function CreatePost() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [excerpt, setExcerpt] = useState("");
  const [published, setPublished] = useState(true);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const [coverPreview, setCoverPreview] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (!user) {
    navigate("/login");
    return null;
  }

  const handleCoverChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setCoverFile(file);
      setCoverPreview(URL.createObjectURL(file));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !content.trim()) {
      toast.error("Title and content are required");
      return;
    }
    setLoading(true);

    let coverUrl: string | null = null;
    if (coverFile) {
      try {
        coverUrl = await uploadApi.image(coverFile);
      } catch {
        toast.error("Failed to upload image");
        setLoading(false);
        return;
      }
    }

    try {
      const data = await blogsApi.create({
        title: title.trim(),
        content: content.trim(),
        excerpt: excerpt.trim() || undefined,
        coverImageUrl: coverUrl,
        published,
      });
      toast.success("Post created!");
      navigate(`/blog/${data.id}`);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to create post");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container max-w-3xl py-10 animate-fade-in">
      {/* Page header */}
      <div className="flex items-center gap-3 mb-8">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <FilePenLine className="h-5 w-5" />
        </div>
        <div>
          <h1 className="text-2xl font-heading font-bold">Write a new post</h1>
          <p className="text-sm text-muted-foreground">Share your ideas with the world</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Cover image */}
        <div className="space-y-2">
          <Label>Cover image</Label>
          <div
            className={`relative border-2 border-dashed rounded-xl transition-all duration-200 cursor-pointer group ${
              coverPreview
                ? "border-primary/40 bg-primary/5 p-0 overflow-hidden"
                : "p-10 text-center hover:border-primary hover:bg-primary/5"
            }`}
            onClick={() => !coverPreview && document.getElementById("cover-input")?.click()}
          >
            {coverPreview ? (
              <>
                <img src={coverPreview} alt="Cover preview" className="w-full h-52 object-cover" />
                <button
                  type="button"
                  onClick={(e) => { e.stopPropagation(); setCoverFile(null); setCoverPreview(null); }}
                  className="absolute top-2 right-2 flex h-7 w-7 items-center justify-center rounded-full bg-background/80 backdrop-blur-sm border border-border hover:bg-destructive hover:text-destructive-foreground hover:border-destructive transition-colors"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              </>
            ) : (
              <div className="flex flex-col items-center gap-3 text-muted-foreground group-hover:text-primary transition-colors">
                <ImagePlus className="h-9 w-9" />
                <div>
                  <p className="font-medium text-sm">Click to upload a cover image</p>
                  <p className="text-xs mt-0.5">PNG, JPG, GIF up to 10MB</p>
                </div>
              </div>
            )}
            <input id="cover-input" type="file" accept="image/*" className="hidden" onChange={handleCoverChange} />
          </div>
        </div>

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
                {published ? "Publish immediately" : "Save as draft"}
              </Label>
              <p className="text-xs text-muted-foreground">
                {published ? "Visible to everyone" : "Only visible to you"}
              </p>
            </div>
          </div>
          <Button type="submit" disabled={loading} className="gap-2 min-w-[120px]">
            {loading ? (
              <><span className="h-4 w-4 rounded-full border-2 border-primary-foreground/30 border-t-primary-foreground animate-spin" /> Saving...</>
            ) : (
              <><Send className="h-4 w-4" /> {published ? "Publish" : "Save draft"}</>
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}
