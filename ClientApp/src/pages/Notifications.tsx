import { useAuth } from "@/contexts/AuthContext";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { notifications as notificationsApi } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { useNavigate } from "react-router-dom";
import { format } from "date-fns";
import { Bell, CheckCheck, MessageSquare, FileText } from "lucide-react";
import { toast } from "sonner";

const typeIcon: Record<string, React.ReactNode> = {
  comment: <MessageSquare className="h-4 w-4 text-primary" />,
  post: <FileText className="h-4 w-4 text-accent" />,
};

export default function Notifications() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const qc = useQueryClient();

  if (!user) { navigate("/login"); return null; }

  const { data: notifications } = useQuery({
    queryKey: ["notifications", user.id],
    queryFn: () => notificationsApi.list(),
  });

  const markAllRead = useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["notifications"] });
      qc.invalidateQueries({ queryKey: ["unread-notifications"] });
      toast.success("All marked as read");
    },
  });

  const markRead = async (notifId: string, blogId?: string | null) => {
    await notificationsApi.markRead(notifId);
    qc.invalidateQueries({ queryKey: ["notifications"] });
    qc.invalidateQueries({ queryKey: ["unread-notifications"] });
    if (blogId) navigate(`/blog/${blogId}`);
  };

  const unreadCount = notifications?.filter((n) => !n.isRead).length ?? 0;

  return (
    <div className="container max-w-2xl py-10 animate-fade-in">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-heading font-bold flex items-center gap-2.5">
            <Bell className="h-6 w-6 text-primary" /> Notifications
          </h1>
          {unreadCount > 0 && (
            <p className="text-sm text-muted-foreground mt-1">{unreadCount} unread</p>
          )}
        </div>
        {unreadCount > 0 && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => markAllRead.mutate()}
            disabled={markAllRead.isPending}
            className="gap-1.5 hover:-translate-y-px transition-transform duration-150"
          >
            <CheckCheck className="h-4 w-4" /> Mark all read
          </Button>
        )}
      </div>

      {notifications?.length ? (
        <div className="space-y-2">
          {notifications.map((n, i) => (
            <div
              key={n.id}
              className={`rounded-xl border p-4 cursor-pointer transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md animate-slide-up ${
                !n.isRead
                  ? "border-primary/30 bg-primary/5 hover:border-primary/50"
                  : "border-border/60 bg-card hover:border-border"
              }`}
              style={{ animationDelay: `${i * 40}ms` }}
              onClick={() => markRead(n.id, n.blogId)}
            >
              <div className="flex items-start gap-3">
                <div className={`mt-0.5 flex h-8 w-8 items-center justify-center rounded-full shrink-0 ${
                  !n.isRead ? "bg-primary/10" : "bg-muted"
                }`}>
                  {typeIcon[n.type] ?? <Bell className="h-4 w-4 text-muted-foreground" />}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-3">
                    <p className={`text-sm font-medium leading-snug ${!n.isRead ? "text-foreground" : "text-foreground/80"}`}>
                      {n.title}
                    </p>
                    <div className="flex items-center gap-2 shrink-0">
                      {!n.isRead && (
                        <span className="h-2 w-2 rounded-full bg-primary shrink-0" />
                      )}
                      <span className="text-xs text-muted-foreground whitespace-nowrap">
                        {format(new Date(n.createdAt), "MMM d")}
                      </span>
                    </div>
                  </div>
                  <p className="text-muted-foreground text-sm mt-0.5">{n.message}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="text-center py-16 border border-dashed border-border rounded-xl">
          <Bell className="h-12 w-12 mx-auto mb-4 opacity-20" />
          <p className="text-muted-foreground">No notifications yet.</p>
          <p className="text-sm text-muted-foreground/60 mt-1">You&apos;ll see activity here when people interact with your posts.</p>
        </div>
      )}
    </div>
  );
}
