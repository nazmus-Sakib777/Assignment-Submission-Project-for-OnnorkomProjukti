import { api } from "./api";
import {
  Assignment,
  ClassRoom,
  Subject,
  Submission,
  User,
  UserRole,
} from "@/types";

// ---- Users (Admin) ----
export const UsersApi = {
  list: (role?: UserRole) =>
    api.get<User[]>("/users", { params: role ? { role } : {} }).then((r) => r.data),
  create: (payload: {
    fullName: string;
    email: string;
    password: string;
    role: UserRole;
    classRoomId?: number | null;
  }) => api.post<User>("/users", payload).then((r) => r.data),
  update: (id: number, payload: { fullName: string; isActive: boolean; classRoomId?: number | null }) =>
    api.put<User>(`/users/${id}`, payload).then((r) => r.data),
  deactivate: (id: number) => api.delete(`/users/${id}`),
};

// ---- Classes & Subjects ----
export const ClassesApi = {
  list: () => api.get<ClassRoom[]>("/classes").then((r) => r.data),
  create: (name: string) => api.post<ClassRoom>("/classes", { name }).then((r) => r.data),
  remove: (id: number) => api.delete(`/classes/${id}`),
  subjects: (classRoomId: number) =>
    api.get<Subject[]>(`/classes/${classRoomId}/subjects`).then((r) => r.data),
  createSubject: (name: string, classRoomId: number) =>
    api.post<Subject>("/classes/subjects", { name, classRoomId }).then((r) => r.data),
  assignTeacher: (teacherId: number, subjectId: number) =>
    api.post("/classes/subjects/assign-teacher", { teacherId, subjectId }),
};

// ---- Assignments ----
export const AssignmentsApi = {
  list: () => api.get<Assignment[]>("/assignments").then((r) => r.data),
  get: (id: number) => api.get<Assignment>(`/assignments/${id}`).then((r) => r.data),
  create: (payload: {
    title: string;
    description: string;
    subjectId: number;
    deadline: string;
    maxMarks: number;
    allowResubmission: boolean;
    publishNow: boolean;
  }) => api.post<Assignment>("/assignments", payload).then((r) => r.data),
  update: (
    id: number,
    payload: { title: string; description: string; deadline: string; maxMarks: number; allowResubmission: boolean }
  ) => api.put<Assignment>(`/assignments/${id}`, payload).then((r) => r.data),
  publish: (id: number) => api.post(`/assignments/${id}/publish`),
  close: (id: number) => api.post(`/assignments/${id}/close`),
  remove: (id: number) => api.delete(`/assignments/${id}`),
};

// ---- Submissions ----
export const SubmissionsApi = {
  submit: (assignmentId: number, answerText: string, attachmentUrl?: string) =>
    api
      .post<Submission>(`/assignments/${assignmentId}/submissions`, { answerText, attachmentUrl })
      .then((r) => r.data),
  update: (submissionId: number, answerText: string, attachmentUrl?: string) =>
    api.put<Submission>(`/submissions/${submissionId}`, { answerText, attachmentUrl }).then((r) => r.data),
  forAssignment: (assignmentId: number) =>
    api.get<Submission[]>(`/assignments/${assignmentId}/submissions`).then((r) => r.data),
  grade: (submissionId: number, marks: number, teacherFeedback?: string) =>
    api.post<Submission>(`/submissions/${submissionId}/grade`, { marks, teacherFeedback }).then((r) => r.data),
};
