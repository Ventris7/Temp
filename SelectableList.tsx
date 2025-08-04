import React, { useState, useEffect, memo } from 'react';
import { List } from 'antd';

type Item = {
  id: number;
  caption: string;
};

type Props = {
  items: Item[];
  onCheckItems?: (ids: number[]) => void;
};

export const SelectableList: React.FC<Props> = ({ items, onCheckItems }) => {
  const [checkedIds, setCheckedIds] = useState<Set<number>>(new Set());

  const handleToggle = (id: number) => {
    setCheckedIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  useEffect(() => {
    if (onCheckItems) {
      onCheckItems(Array.from(checkedIds));
    }
  }, [checkedIds, onCheckItems]);

  return (
    <List
      bordered
      dataSource={items}
      renderItem={(item) => {
        // console.log(item.id); - здесь это не нужно вызывать, т.к. renderItem вызывается для каждого элемента списка, после вызова setCheckedIds,
        // но реальный ререндер происходит только для изменившихся элементов благодаря memoization.
        return (
          <MemoListItem
            key={item.id}
            item={item}
            checked={checkedIds.has(item.id)}
            onToggle={handleToggle}
          />
        );
      }}
    />
  );
};

type ListItemProps = {
  item: Item;
  checked: boolean;
  onToggle: (id: number) => void;
};

const ListItem: React.FC<ListItemProps> = ({ item, checked, onToggle }) => {
  console.log(item.id); // вот здесь правильно тестировать кол-во рендеров, выводится только для кликнутого элемента, т.к. изменяется его состояние
  return (
    <List.Item
      style={{
        backgroundColor: checked ? '#ff4d4f' : undefined,
        color: checked ? 'white' : undefined,
        cursor: 'pointer',
        transition: 'background-color 0.2s ease',
      }}
      onClick={() => onToggle(item.id)}
    >
      {item.caption}
    </List.Item>
  );
};

const MemoListItem = memo(
  ListItem,
  // Здесь мы сравниваем только те свойства, которые нам важны для ререндеринга
  (prev, next) =>
    prev.checked === next.checked &&
    prev.item.id === next.item.id &&
    prev.item.caption === next.item.caption,
);
