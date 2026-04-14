import { Link, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "@/contexts/AuthContext";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Bell, PenSquare, LogOut, User, Shield } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { notifications as notificationsApi } from "@/lib/api";
import { ReactNode, useEffect, useState } from "react";

export default function Layout({ children }: { children: ReactNode }) {
  const { user, profile, isAdmin, signOut } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [scrolled, setScrolled] = useState(false);
  const [readingProgress, setReadingProgress] = useState(0);
  const isBlogDetail = location.pathname.startsWith("/blog/");

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 8);
      if (isBlogDetail) {
        const doc = document.documentElement;
        const scrollTop = window.scrollY;
        const scrollHeight = doc.scrollHeight - doc.clientHeight;
        setReadingProgress(scrollHeight > 0 ? (scrollTop / scrollHeight) * 100 : 0);
      }
    };
    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, [isBlogDetail]);

  useEffect(() => {
    setReadingProgress(0);
    window.scrollTo({ top: 0, behavior: "instant" });
  }, [location.pathname]);

  const { data: unreadCount } = useQuery({
    queryKey: ["unread-notifications", user?.id],
    queryFn: async () => {
      if (!user) return 0;
      const items = await notificationsApi.list();
      return items.filter((n) => !n.isRead).length;
    },
    enabled: !!user,
    refetchInterval: 30000,
  });

  const initials = profile?.displayName
    ? profile.displayName.slice(0, 2).toUpperCase()
    : "?";

  return (
    <div className="min-h-screen bg-background">
      {isBlogDetail && (
        <div className="reading-progress" style={{ width: `${readingProgress}%` }} />
      )}

      <header
        className={`sticky top-0 z-50 border-b transition-all duration-300 ${
          scrolled
            ? "bg-card/95 backdrop-blur-md shadow-sm shadow-foreground/5"
            : "bg-card/80 backdrop-blur-sm"
        }`}
      >
        <div className="container flex h-16 items-center justify-between">
          <Link to="/" className="flex items-center gap-2 group">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-navy text-navy-foreground font-heading font-bold text-sm ring-0 group-hover:ring-2 ring-primary/40 transition-all duration-200">
              W
            </div>
            <span className="font-heading text-xl font-bold text-foreground group-hover:text-primary transition-colors duration-200">
              Weblog
            </span>
          </Link>

          <nav className="flex items-center gap-2">
            {user ? (
              <>
                <Button
                  variant="ghost"
                  size="icon"
                  className="relative hover:bg-primary/10 transition-colors"
                  onClick={() => navigate("/notifications")}
                  aria-label="Notifications"
                >
                  <Bell className="h-5 w-5" />
                  {(unreadCount ?? 0) > 0 && (
                    <span className="absolute -top-0.5 -right-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-primary text-[10px] text-primary-foreground font-bold">
                      <span className="absolute inline-flex h-full w-full rounded-full bg-primary opacity-60 animate-ping" />
                      {unreadCount}
                    </span>
                  )}
                </Button>
                <Button
                  variant="default"
                  size="sm"
                  className="gap-1.5 shadow-sm hover:shadow-md hover:-translate-y-px transition-all duration-200"
                  onClick={() => navigate("/create")}
                >
                  <PenSquare className="h-4 w-4" />
                  Write
                </Button>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="rounded-full ring-2 ring-transparent hover:ring-primary/30 transition-all duration-200"
                    >
                      <Avatar className="h-8 w-8">
                        <AvatarImage src={profile?.avatarUrl ?? undefined} />
                        <AvatarFallback className="bg-primary text-primary-foreground text-xs">
                          {initials}
                        </AvatarFallback>
                      </Avatar>
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48">
                    <div className="px-2 py-1.5 text-xs text-muted-foreground font-medium truncate">
                      {profile?.displayName ?? user.email}
                    </div>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={() => navigate("/profile")}>
                      <User className="mr-2 h-4 w-4" /> Profile
                    </DropdownMenuItem>
                    {isAdmin && (
                      <DropdownMenuItem onClick={() => navigate("/admin")}>
                        <Shield className="mr-2 h-4 w-4" /> Admin
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={signOut} className="text-destructive focus:text-destructive">
                      <LogOut className="mr-2 h-4 w-4" /> Sign out
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </>
            ) : (
              <div className="flex gap-2">
                <Button
                  variant="ghost"
                  size="sm"
                  className="hover:bg-primary/10 transition-colors"
                  onClick={() => navigate("/login")}
                >
                  Log in
                </Button>
                <Button
                  size="sm"
                  className="shadow-sm hover:shadow-md hover:-translate-y-px transition-all duration-200"
                  onClick={() => navigate("/signup")}
                >
                  Sign up
                </Button>
              </div>
            )}
          </nav>
        </div>
      </header>

      <main className="min-h-[calc(100vh-4rem)]">{children}</main>

      <footer className="border-t bg-navy text-navy-foreground py-12 mt-24">
        <div className="container">
          <div className="flex flex-col md:flex-row justify-between items-start gap-8">
            <div>
              <Link to="/" className="flex items-center gap-2 mb-3 group">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-heading font-bold text-xs">
                  W
                </div>
                <span className="font-heading font-bold text-lg group-hover:text-primary transition-colors">Weblog</span>
              </Link>
              <p className="text-sm text-navy-foreground/50 max-w-xs">
                A space to share ideas, stories, and knowledge with the world.
              </p>
            </div>
            <div className="flex gap-12 text-sm">
              <div className="space-y-3">
                <p className="font-semibold text-navy-foreground/80 uppercase tracking-wide text-xs">Explore</p>
                <div className="flex flex-col gap-2 text-navy-foreground/50">
                  <Link to="/" className="hover:text-navy-foreground transition-colors">Home</Link>
                  {user && <Link to="/create" className="hover:text-navy-foreground transition-colors">Write</Link>}
                  {user && <Link to="/profile" className="hover:text-navy-foreground transition-colors">Profile</Link>}
                </div>
              </div>
              {!user && (
                <div className="space-y-3">
                  <p className="font-semibold text-navy-foreground/80 uppercase tracking-wide text-xs">Account</p>
                  <div className="flex flex-col gap-2 text-navy-foreground/50">
                    <Link to="/login" className="hover:text-navy-foreground transition-colors">Log in</Link>
                    <Link to="/signup" className="hover:text-navy-foreground transition-colors">Sign up</Link>
                  </div>
                </div>
              )}
            </div>
          </div>
          <div className="border-t border-navy-foreground/10 mt-8 pt-6 text-xs text-navy-foreground/40 text-center md:text-left">
            &copy; {new Date().getFullYear()} Weblog. All rights reserved.
          </div>
        </div>
      </footer>
    </div>
  );
}
