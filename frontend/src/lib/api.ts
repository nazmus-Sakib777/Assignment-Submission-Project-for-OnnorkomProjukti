import axios from "axios";

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000/api";

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { "Content-Type": "application/json" },
});

// Attach the JWT (if present) to every outgoing request.
api.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("asms_token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

// On a 401, clear the stored session and bounce to login.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (
      typeof window !== "undefined" &&
      error?.response?.status === 401 &&
      !window.location.pathname.startsWith("/login")
    ) {
      localStorage.removeItem("asms_token");
      localStorage.removeItem("asms_user");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

export function extractErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data;
    if (data?.error) return data.error;
    if (data?.title) return data.title;
    if (data?.errors) {
      const first = Object.values(data.errors)[0];
      if (Array.isArray(first) && first.length) return first[0];
    }
    if (err.message) return err.message;
  }
  return "Something went wrong. Please try again.";
}
