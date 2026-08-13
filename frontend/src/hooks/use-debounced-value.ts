import { useEffect, useState } from 'react';

/**
 * Değeri belirtilen süre kadar geciktirir. Arama girdisinde her tuş vuruşunda
 * istek atmayı önlemek için kullanılır.
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}
