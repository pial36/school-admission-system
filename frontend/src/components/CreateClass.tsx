import { useState, type FormEvent } from 'react';
import { api } from '../api';
import { ErrorBox, SuccessBox } from './common';

/** Screen: Class create (school admin). */
export function CreateClass() {
  const [name, setName] = useState('');
  const [seats, setSeats] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const seatsNumber = Number(seats);
  const seatsValid = /^\d+$/.test(seats) && seatsNumber >= 1 && seatsNumber <= 10000;
  const canSubmit = name.trim().length > 0 && seatsValid && !busy;

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!canSubmit) return;
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const created = await api.createClass(name.trim(), seatsNumber);
      setSuccess(`Class "${created.name}" created with ${created.totalSeats} seats.`);
      setName('');
      setSeats('');
    } catch (err) {
      setError(err as Error);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="card">
      <h2>Create a class <span className="muted small">(school admin)</span></h2>
      <p className="muted">
        Each shift is a separate class, e.g. “Class 6 – Morning Shift” and “Class 6 – Day Shift”.
      </p>
      <form onSubmit={submit} className="form">
        <label>
          Class name
          <input value={name} maxLength={200} placeholder="Class 6 – Morning Shift"
                 onChange={(e) => setName(e.target.value)} />
        </label>
        <label>
          Total seats
          <input value={seats} inputMode="numeric" placeholder="120"
                 onChange={(e) => setSeats(e.target.value.replace(/\D/g, ''))} />
          {seats !== '' && !seatsValid && <span className="field-error">Enter a number from 1 to 10000.</span>}
        </label>
        <button type="submit" className="btn btn-primary" disabled={!canSubmit}>
          {busy ? 'Creating…' : 'Create class'}
        </button>
      </form>
      <ErrorBox error={error} />
      <SuccessBox message={success} />
    </section>
  );
}
