export type UserRole = "Admin" | "Teacher" | "Student";

export type AssignmentStatus = "Draft" | "Published" | "Closed";

export type SubmissionStatus =
  | "Submitted"
  | "Resubmitted"
  | "Late"
  | "Graded"
  | "Returned";

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  classRoomId: number | null;
  classRoomName: string | null;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface ClassRoom {
  id: number;
  name: string;
  studentCount: number;
  subjectCount: number;
}

export interface Subject {
  id: number;
  name: string;
  classRoomId: number;
  classRoomName: string;
}

export interface Submission {
  id: number;
  assignmentId: number;
  assignmentTitle: string;
  studentId: number;
  studentName: string;
  answerText: string;
  attachmentUrl: string | null;
  status: SubmissionStatus;
  marks: number | null;
  maxMarks: number | null;
  teacherFeedback: string | null;
  submittedAt: string;
  updatedAt: string | null;
  gradedAt: string | null;
  isLate: boolean;
}

export interface Assignment {
  id: number;
  title: string;
  description: string;
  subjectId: number;
  subjectName: string;
  classRoomId: number;
  classRoomName: string;
  createdByTeacherId: number;
  createdByTeacherName: string;
  deadline: string;
  maxMarks: number;
  status: AssignmentStatus;
  allowResubmission: boolean;
  createdAt: string;
  submissionCount: number;
  mySubmission: Submission | null;
}

export interface ApiError {
  error?: string;
  title?: string;
  errors?: Record<string, string[]>;
}
