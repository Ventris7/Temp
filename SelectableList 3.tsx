Файл SelectableList.tsx:

import React, { useEffect, useMemo, useRef, useState } from 'react';
import { List } from 'antd';
import { equalSets } from 'src/shared/utils/extensions';
import styles from './SelectableList.module.scss';
import { Row } from './SelectableListRow';

export type SelectableListProps<T> = {
  items: readonly T[];
  multiselect?: boolean;
  control?: ReadonlySet<number>;
  defaultSelection?: ReadonlySet<number>;
  getKey: (item: T) => number;
  renderItem: (item: T) => React.ReactNode;
  getHash: (item: T) => string;
  onChange?: (next: ReadonlySet<number>) => void;
};

function useControllableSelection(
  controlled: ReadonlySet<number> | undefined,
  defaultSet: ReadonlySet<number> | undefined,
) {
  const [selection, setSelection] = useState<ReadonlySet<number>>(
    () => controlled ?? defaultSet ?? new Set(),
  );

  useEffect(() => {
    if (controlled) setSelection(controlled);
  }, [controlled]);

  return [controlled ?? selection, setSelection] as const;
}

export const SelectableList = <T,>(props: SelectableListProps<T>) => {
  const { items, multiselect, control, defaultSelection, getKey, renderItem, getHash, onChange } =
    props;
  const [selection, setSelection] = useControllableSelection(control, defaultSelection);
  const onChangeRef = useRef(onChange);
  const getKeyRef = useRef(getKey);

  console.log(selection);

  // Удаление из выбранных тех которых нет в items, если items пришли новые.
  useEffect(() => {
    setSelection((prev) => {
      const next = new Set(prev);
      const keys = items.map((item) => getKeyRef.current(item));

      prev.forEach((key) => {
        if (!keys.includes(key)) next.delete(key);
      });

      if (!equalSets(next, prev)) return next;
      return prev;
    });
  }, [items, setSelection]);

  // Вызываем onChanged если selection изменился.
  useEffect(() => {
    onChangeRef.current?.(selection);
  }, [selection]);

  // Формируем строки из items, добавляем к ним key и isSelected,
  // и "замораживаем" их, пока не изменятся items или selection.
  const rows = useMemo(() => {
    return items.map((item) => {
      const key = getKeyRef.current(item);
      const isSelected = selection.has(key);
      return { key, item, isSelected } as const;
    });
  }, [items, selection]);

  // Обновляет selection по клику на строку (для разных режимов по multiselect значению).
  const toggleSelected = (key: number) => {
    setSelection((prev) => {
      const next = new Set(prev);

      if (multiselect) {
        if (next.has(key)) next.delete(key);
        else next.add(key);
      } else {
        if (next.has(key) && next.size === 1) {
          next.clear();
        } else {
          next.clear();
          next.add(key);
        }
      }
      return next;
    });
  };

  return (
    <List className={styles.list} aria-multiselectable={multiselect || undefined}>
      {rows.map(({ key, item, isSelected }) => (
        <Row
          key={key}
          item={item}
          selected={isSelected}
          onClick={() => toggleSelected(key)}
          getHash={() => getHash(item)}
          renderItem={renderItem}
        />
      ))}
    </List>
  );
};

Файл SelectableListRow.tsx

import { memo } from 'react';
import { List } from 'antd';
import styles from './SelectableListRow.module.scss';

type RowProps<T> = {
  item: T;
  selected: boolean;
  onClick: () => void;
  renderItem: (item: T) => React.ReactNode;
  getHash: () => string;
};

export const RowInner = <T,>(props: RowProps<T>) => {
  const { item, selected, onClick, renderItem } = props;
  console.log(item);

  return (
    <List.Item
      role="option"
      aria-selected={selected}
      onClick={onClick}
      className={selected ? styles.selectedListItem : styles.listItem}
    >
      {renderItem(item)}
    </List.Item>
  );
};

export const Row = memo(RowInner, (prev, next) => {
  return prev.selected === next.selected && prev.getHash() === next.getHash();
}) as <T>(props: RowProps<T>) => React.ReactElement;
