import { isValidStudentId } from '../hooks';

/** The box at the top of every page where the student's 17-digit ID is typed. */
export function StudentBar({ studentId, onChange }: { studentId: string; onChange: (v: string) => void }) {
  const valid = isValidStudentId(studentId);
  return (
    <div className="student-bar">
      <label htmlFor="student-id">Student birth registration number</label>
      <input
        id="student-id"
        inputMode="numeric"
        autoComplete="off"
        placeholder="17 digits, e.g. 20152691234567890"
        value={studentId}
        onChange={(e) => onChange(e.target.value)}
        aria-invalid={studentId.length > 0 && !valid}
        className={studentId.length === 0 ? '' : valid ? 'valid' : 'invalid'}
      />
      <span className={`hint ${valid ? 'ok' : ''}`}>
        {valid ? '✓ Valid' : `${studentId.length}/17 digits`}
      </span>
    </div>
  );
}
