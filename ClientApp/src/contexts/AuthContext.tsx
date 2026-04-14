import { createContext, useContext, useEffect, useState, ReactNode } from "react";
import { auth as authApi, setToken, clearToken, getToken } from "@/lib/api";
import type { AuthUser, UserProfile } from "@/lib/types";

interface AuthContextType {
  user: AuthUser | null;
  profile: UserProfile | null;
  isAdmin: boolean;
  loading: boolean;
  signOut: () => void;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const [loading, setLoading] = useState(true);

  const refreshProfile = async () => {
    try {
      const data = await authApi.me();
      setUser(data.user);
      setProfile(data.profile);
      setIsAdmin(data.isAdmin);
    } catch {
      clearToken();
      setUser(null);
      setProfile(null);
      setIsAdmin(false);
    }
  };

  useEffect(() => {
    const token = getToken();
    if (token) {
      refreshProfile().finally(() => setLoading(false));
    } else {
      setLoading(false);
    }

    const handleLogout = () => {
      setUser(null);
      setProfile(null);
      setIsAdmin(false);
    };
    window.addEventListener("auth:logout", handleLogout);
    return () => window.removeEventListener("auth:logout", handleLogout);
  }, []);

  const signOut = () => {
    clearToken();
    setUser(null);
    setProfile(null);
    setIsAdmin(false);
  };

  return (
    <AuthContext.Provider value={{ user, profile, isAdmin, loading, signOut, refreshProfile }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

// Helper to apply a successful auth response from the API
export function applyAuthResponse(response: {
  token: string;
  user: AuthUser;
  profile: UserProfile;
  isAdmin: boolean;
}) {
  setToken(response.token);
}
