"use client";

import {
  createContext,
  useContext,
  useEffect,
  useState,
  ReactNode,
  useCallback,
} from "react";
import { useRouter } from "next/navigation";
import { api, extractErrorMessage } from "./api";
import { LoginResponse, User } from "@/types";

interface AuthContextValue {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const stored = localStorage.getItem("asms_user");
    const token = localStorage.getItem("asms_token");
    if (stored && token) {
      try {
        setUser(JSON.parse(stored));
      } catch {
        localStorage.removeItem("asms_user");
      }
    }
    setLoading(false);
  }, []);

  const login = useCallback(
    async (email: string, password: string) => {
      try {
        const { data } = await api.post<LoginResponse>("/auth/login", {
          email,
          password,
        });
        localStorage.setItem("asms_token", data.token);
        localStorage.setItem("asms_user", JSON.stringify(data.user));
        setUser(data.user);

        const dest =
          data.user.role === "Admin"
            ? "/admin"
            : data.user.role === "Teacher"
            ? "/teacher"
            : "/student";
        router.push(dest);
      } catch (err) {
        throw new Error(extractErrorMessage(err));
      }
    },
    [router]
  );

  const logout = useCallback(() => {
    localStorage.removeItem("asms_token");
    localStorage.removeItem("asms_user");
    setUser(null);
    router.push("/login");
  }, [router]);

  return (
    <AuthContext.Provider value={{ user, loading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
