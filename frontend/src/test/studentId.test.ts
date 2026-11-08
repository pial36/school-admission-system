import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { isValidStudentId, useStudentId } from '../hooks';

describe('isValidStudentId', () => {
  it('accepts exactly 17 digits only', () => {
    expect(isValidStudentId('20152691234567890')).toBe(true);
    expect(isValidStudentId('2015269123456789')).toBe(false);   // 16
    expect(isValidStudentId('201526912345678901')).toBe(false); // 18
    expect(isValidStudentId('2015269123456789X')).toBe(false);
  });
});

describe('useStudentId', () => {
  it('keeps only digits, max 17, and remembers the value across reloads', () => {
    const { result } = renderHook(() => useStudentId());

    act(() => result.current[1]('2015-2691 2345abc6789012345'));
    expect(result.current[0]).toBe('20152691234567890');

    // A fresh hook (= page reload) reads it back from localStorage.
    const { result: afterReload } = renderHook(() => useStudentId());
    expect(afterReload.current[0]).toBe('20152691234567890');
  });
});
