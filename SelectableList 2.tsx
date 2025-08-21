const prevKeysRef = useRef<Set<number> | null>(null);

useEffect(() => {
  // В контролируемом режиме ничего не трогаем
  if (control) return;

  const keysNow = new Set(items.map((it) => getKeyRef.current!(it)));

  if (items.length === 0) {
    // Если это первый рендер (prevKeysRef ещё null) → ничего не делаем,
    // даём отработать defaultSelection из useState
    if (prevKeysRef.current === null) {
      prevKeysRef.current = keysNow;
      return;
    }
    // Иначе (после загрузки и очистки списка) сбрасываем выбор
    setSelection((prev) => (prev.size === 0 ? prev : new Set()));
    prevKeysRef.current = keysNow;
    return;
  }

  // Чистим только если набор ключей реально изменился
  const prevKeys = prevKeysRef.current;
  prevKeysRef.current = keysNow;

  const keysUnchanged =
    prevKeys !== null &&
    prevKeys.size === keysNow.size &&
    [...keysNow].every((k) => prevKeys.has(k));

  if (keysUnchanged) return;

  setSelection((prev) => {
    const next = new Set([...prev].filter((k) => keysNow.has(k)));
    return equalSets(next, prev) ? prev : next;
  });
}, [items, control, setSelection]);
