// Distinguish authentication, linked-profile denial and temporary load failures.
// The shared transport has already attempted its normal refresh before a 401.
export class TeacherWorkspaceError extends Error {
  constructor(public readonly status: number) {
    super("Teacher workspace request failed");
  }
}

export function teacherWorkspaceFailure(error: unknown) {
  if (error instanceof TeacherWorkspaceError && error.status === 401) {
    return { signIn: true, message: "Please sign in to open your Teacher workspace. Your session may have expired." };
  }
  if (error instanceof TeacherWorkspaceError && error.status === 403) {
    return { signIn: false, message: "This account is not linked to an active teacher profile. Please contact your Academy Admin." };
  }
  return { signIn: false, message: "Your teaching workspace could not be loaded. Check your connection and reload to try again." };
}
