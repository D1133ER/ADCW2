import { useState } from "react";
import { Link } from "react-router-dom";
import { auth as authApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { ArrowLeft, Mail, CheckCircle2 } from "lucide-react";

export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);

  const handleReset = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await authApi.forgotPassword(email);
    } catch {
      // Always show success to avoid email enumeration
    } finally {
      setLoading(false);
      setSent(true);
    }
    toast.success("If an account exists with that email, you'll receive a reset link.");
  };

  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center px-4 py-12">
      <div className="absolute inset-0 -z-10 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-primary/5 blur-3xl" />
      </div>

      <div className="w-full max-w-md animate-fade-scale">
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-2 group mb-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-navy text-navy-foreground font-heading font-bold group-hover:ring-2 ring-primary/40 transition-all duration-200">W</div>
            <span className="font-heading text-2xl font-bold">Weblog</span>
          </Link>
          <h1 className="text-3xl font-heading font-bold mb-1">Reset password</h1>
          <p className="text-muted-foreground">We&apos;ll send you a link to reset it</p>
        </div>

        <div className="bg-card border border-border/60 rounded-2xl shadow-lg shadow-foreground/5 p-8">
          {sent ? (
            <div className="text-center py-4">
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-accent/10 text-accent mx-auto mb-5">
                <CheckCircle2 className="h-8 w-8" />
              </div>
              <h2 className="text-xl font-heading font-semibold mb-2">Email sent!</h2>
              <p className="text-muted-foreground text-sm mb-6">Check your inbox for a password reset link. It may take a minute to arrive.</p>
              <Link to="/login" className="inline-flex items-center gap-1.5 text-sm text-primary hover:underline font-medium">
                <ArrowLeft className="h-4 w-4" /> Back to login
              </Link>
            </div>
          ) : (
            <form onSubmit={handleReset} className="space-y-5">
              <div className="space-y-2">
                <Label htmlFor="email" className="text-sm font-medium">Email address</Label>
                <Input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  placeholder="you@example.com"
                  className="h-11"
                />
              </div>
              <Button type="submit" className="w-full h-11 gap-2 font-semibold" disabled={loading}>
                {loading ? (
                  <span className="flex items-center gap-2">
                    <span className="h-4 w-4 rounded-full border-2 border-primary-foreground/30 border-t-primary-foreground animate-spin" />
                    Sending...
                  </span>
                ) : (
                  <span className="flex items-center gap-2"><Mail className="h-4 w-4" /> Send reset link</span>
                )}
              </Button>
              <Link to="/login" className="flex items-center justify-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors">
                <ArrowLeft className="h-3.5 w-3.5" /> Back to login
              </Link>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
