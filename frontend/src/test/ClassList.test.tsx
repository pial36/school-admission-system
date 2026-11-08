import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError } from '../api';
import { ClassList } from '../components/ClassList';

describe('ClassList', () => {
  beforeEach(() => {
    vi.spyOn(api, 'listClasses').mockResolvedValue([
      { id: 'c1', name: 'Class 6 - Morning Shift', totalSeats: 120, heldSeats: 15, confirmedSeats: 80, availableSeats: 25 },
      { id: 'c2', name: 'Class 7 - Day Shift', totalSeats: 50, heldSeats: 10, confirmedSeats: 40, availableSeats: 0 },
    ]);
  });

  it('shows seat counts and disables Apply for a full class', async () => {
    render(<ClassList studentId="" />);

    expect(await screen.findByText('Class 6 - Morning Shift')).toBeInTheDocument();
    expect(screen.getByText('25')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Full' })).toBeDisabled();
  });

  it('asks for the student ID before applying', async () => {
    const apply = vi.spyOn(api, 'apply');
    render(<ClassList studentId="" />);

    await userEvent.click(await screen.findByRole('button', { name: 'Apply' }));

    expect(screen.getByRole('alert')).toHaveTextContent(/17-digit birth registration number/);
    expect(apply).not.toHaveBeenCalled();
  });

  it('shows the server error in plain language when seats run out', async () => {
    vi.spyOn(api, 'apply').mockRejectedValue(
      new ApiError(409, 'SEATS_FULL', 'Sorry, all seats in this class are taken right now.'));
    render(<ClassList studentId="20152691234567890" />);

    await userEvent.click(await screen.findByRole('button', { name: 'Apply' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Sorry, all seats in this class are taken right now.');
  });
});
