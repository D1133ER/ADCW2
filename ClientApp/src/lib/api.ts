/**
 * HTTP client for the ASP.NET Core Web API.
 *
 * Base URL is configured via the VITE_API_URL environment variable.
 * JWT is stored in localStorage under the key "auth_token".
 *
 * The contract expected from the backend:
 *
 *  POST   /api/auth/register        { email, displayName, password }  → AuthResponse
 *  POST   /api/auth/login           { email, password }               → AuthResponse
 *  POST   /api/auth/forgot-password { email }                         → 204
 *  POST   /api/auth/reset-password  { token, newPassword }            → 204
 *
 *  GET    /api/blogs                ?published=true                   → Blog[]
 *  GET    /api/blogs/{id}                                             → Blog (with profiles + comments)
 *  POST   /api/blogs                { title, content, excerpt?, coverImageUrl?, published }  → Blog
 *  PUT    /api/blogs/{id}           { title, content, excerpt?, published }                  → Blog
 *  DELETE /api/blogs/{id}                                             → 204
 *
 *  GET    /api/blogs/{id}/comments                                    → Comment[]
 *  POST   /api/blogs/{id}/comments  { content }                       → Comment
 *  POST   /api/comments/{id}/vote   { voteType: 1 | -1 }             → 200
 *
 *  GET    /api/users/me/blogs                                         → Blog[]
 *  GET    /api/auth/me                                                → MeResponse
 *  PUT    /api/auth/me/profile      { displayName, bio }             → UserProfile
 *
 *  GET    /api/notifications                                          → Notification[]
 *  PUT    /api/notifications/read-all                                 → 204
 *  PUT    /api/notifications/{id}/read                                → 204
 *
 *  GET    /api/admin/stats                                            → AdminStats
 *  GET    /api/admin/users          ?limit=10                         → UserProfile[]
 *  GET    /api/admin/blogs          ?limit=10                         → Blog[]
 *
 *  POST   /api/upload/image         FormData { file }                 → { url: string }
 */

import type {
  Blog,
  Comment,
  Notification,
  UserProfile,
  AuthUser,
  AdminStats,
} from "@/lib/types";

// ─── Config ──────────────────────────────────────────────────────────────────

const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined)?.trim() || "";
const TOKEN_KEY = "auth_token";

// ─── Token helpers ────────────────────────────────────────────────────────────

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

// ─── Core fetch wrapper ───────────────────────────────────────────────────────

type RequestOptions = Omit<RequestInit, "body"> & { body?: unknown };

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string> | undefined),
  };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (res.status === 401) {
    clearToken();
    window.dispatchEvent(new Event("auth:logout"));
    throw new Error("Unauthorized");
  }

  if (!res.ok) {
    let message = `Request failed: ${res.status}`;
    try {
      const json = await res.json();
      message = json?.message ?? json?.title ?? message;
    } catch {}
    throw new Error(message);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// ─── Multipart upload (images) ────────────────────────────────────────────────

export async function uploadImage(file: File): Promise<string> {
  const token = getToken();
  const form = new FormData();
  form.append("file", file);

  const res = await fetch(`${BASE_URL}/api/upload/image`, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: form,
  });
  if (!res.ok) throw new Error("Image upload failed");
  const data: { url: string } = await res.json();
  return data.url;
}

export const uploadApi = {
  image: uploadImage,
};

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface AuthResponse {
  token: string;
  user: AuthUser;
  profile: UserProfile;
  isAdmin: boolean;
}

export interface MeResponse {
  user: AuthUser;
  profile: UserProfile;
  isAdmin: boolean;
}

export const auth = {
  login: (email: string, password: string) =>
    request<AuthResponse>("/api/auth/login", { method: "POST", body: { email, password } }),

  register: (email: string, password: string, displayName: string) =>
    request<AuthResponse>("/api/auth/register", {
      method: "POST",
      body: { email, password, displayName },
    }),

  me: () => request<MeResponse>("/api/auth/me"),

  forgotPassword: (email: string) =>
    request<void>("/api/auth/forgot-password", { method: "POST", body: { email } }),

  resetPassword: (token: string, newPassword: string) =>
    request<void>("/api/auth/reset-password", { method: "POST", body: { token, newPassword } }),

  updateProfile: (displayName: string, bio: string) =>
    request<UserProfile>("/api/auth/me/profile", {
      method: "PUT",
      body: { displayName, bio },
    }),
};

// ─── Blogs ────────────────────────────────────────────────────────────────────

export const blogs = {
  list: (published?: boolean) => {
    const qs = published !== undefined ? `?published=${published}` : "";
    return request<Blog[]>(`/api/blogs${qs}`);
  },

  get: (id: string) => request<Blog>(`/api/blogs/${id}`),

  getComments: (id: string) => request<Comment[]>(`/api/blogs/${id}/comments`),

  create: (payload: {
    title: string;
    content: string;
    excerpt?: string;
    coverImageUrl?: string | null;
    published: boolean;
  }) => request<Blog>("/api/blogs", { method: "POST", body: payload }),

  update: (
    id: string,
    payload: { title: string; content: string; excerpt?: string; published: boolean }
  ) => request<Blog>(`/api/blogs/${id}`, { method: "PUT", body: payload }),

  delete: (id: string) => request<void>(`/api/blogs/${id}`, { method: "DELETE" }),

  myBlogs: () => request<Blog[]>("/api/users/me/blogs"),
};

// ─── Comments ─────────────────────────────────────────────────────────────────

export const comments = {
  list: (blogId: string) => request<Comment[]>(`/api/blogs/${blogId}/comments`),

  add: (blogId: string, content: string) =>
    request<Comment>(`/api/blogs/${blogId}/comments`, { method: "POST", body: { content } }),

  vote: (commentId: string, voteType: 1 | -1) =>
    request<void>(`/api/comments/${commentId}/vote`, { method: "POST", body: { voteType } }),
};

// ─── Notifications ────────────────────────────────────────────────────────────

export const notifications = {
  list: () => request<Notification[]>("/api/notifications"),

  markAllRead: () =>
    request<void>("/api/notifications/read-all", { method: "PUT" }),

  markRead: (id: string) =>
    request<void>(`/api/notifications/${id}/read`, { method: "PUT" }),
};

// ─── Admin ────────────────────────────────────────────────────────────────────

export const admin = {
  stats: () => request<AdminStats>("/api/admin/stats"),

  recentUsers: (limit = 10) =>
    request<UserProfile[]>(`/api/admin/users?limit=${limit}`),

  recentBlogs: (limit = 10) =>
    request<Blog[]>(`/api/admin/blogs?limit=${limit}`),
};
