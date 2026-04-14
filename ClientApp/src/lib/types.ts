// ─── Domain models matching the ASP.NET API response shapes ──────────────────

export interface UserProfile {
  id: string;
  userId: string;
  displayName: string;
  bio: string | null;
  avatarUrl: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AuthUser {
  id: string;
  email: string;
}

export interface Blog {
  id: string;
  userId: string;
  title: string;
  content: string;
  excerpt: string | null;
  coverImageUrl: string | null;
  published: boolean;
  isFeatured: boolean;
  createdAt: string;
  updatedAt: string;
  // joined
  profiles?: Pick<UserProfile, "displayName" | "avatarUrl">;
}

export interface CommentVote {
  id: string;
  commentId: string;
  userId: string;
  voteType: 1 | -1;
  createdAt: string;
}

export interface Comment {
  id: string;
  blogId: string;
  userId: string;
  content: string;
  parentId: string | null;
  createdAt: string;
  updatedAt: string;
  // joined
  profiles?: Pick<UserProfile, "displayName" | "avatarUrl">;
  commentVotes?: CommentVote[];
}

export interface Notification {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  blogId: string | null;
  createdAt: string;
}

export interface AdminStats {
  users: number;
  blogs: number;
  comments: number;
}
