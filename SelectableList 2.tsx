import React, {useCallback, useMemo, useRef, useState} from "react";

/**

Generic, high‑performance selectable list for React 19.

✅ Items are provided via props

✅ Works with any item type via generics

✅ Rows highlight when selected

✅ Rows do NOT re-render unless their item reference or selection state changes

Usage example at the bottom of the file (Demo component is the default export for live preview). */


export type KeyLike = string | number;

export type SelectableListProps<T> = { items: readonly T[]; /** Unique, stable key for an item / getKey: (item: T) => KeyLike; /* Render function for an item row / renderItem: (item: T) => React.ReactNode; /* Controlled selection (optional). If provided, component becomes controlled. / selectedKeys?: ReadonlySet<KeyLike>; /* Initial selection for uncontrolled mode. / defaultSelectedKeys?: ReadonlySet<KeyLike>; /* Called whenever selection changes. / onChange?: (next: ReadonlySet<KeyLike>) => void; /* Allow multiple selection. Defaults to true. / multiselect?: boolean; /* Optional className for the list container. */ className?: string; };

function useControllableSelection( controlled: ReadonlySet<KeyLike> | undefined, defaultSet: ReadonlySet<KeyLike> | undefined ) { const [inner, setInner] = useState<ReadonlySet<KeyLike>>( () => controlled ?? defaultSet ?? new Set() );

// Keep internal state in sync when switching to controlled mode React.useEffect(() => { if (controlled) setInner(controlled); }, [controlled]);

return [controlled ?? inner, setInner] as const; }

/** A single row that only re-renders when its item reference or selection state changes. */ const Row = React.memo( <T,>(props: { item: T; selected: boolean; onToggle: () => void; renderItem: (item: T) => React.ReactNode; }) => { const { item, selected, onToggle, renderItem } = props;

return (
  <li
    role="option"
    aria-selected={selected}
    onClick={onToggle}
    className={
      "cursor-pointer select-none rounded-xl px-3 py-2 transition-colors " +
      (selected
        ? "bg-blue-100 ring-2 ring-blue-300"
        : "hover:bg-gray-50")
    }
  >
    {renderItem(item)}
  </li>
);

}, // Custom comparator — ignore function identity for renderItem (prev, next) => prev.item === next.item && prev.selected === next.selected );

export function SelectableList<T>(props: SelectableListProps<T>) { const { items, getKey, renderItem, selectedKeys: controlledKeys, defaultSelectedKeys, onChange, multiselect = true, className, } = props;

const [selected, setSelected] = useControllableSelection( controlledKeys, defaultSelectedKeys );

// Keep a stable ref to the latest onChange to avoid re-rendering rows const onChangeRef = useRef(onChange); onChangeRef.current = onChange;

const toggleKey = useCallback( (key: KeyLike) => { const next = new Set(selected);

if (multiselect) {
    if (next.has(key)) next.delete(key);
    else next.add(key);
  } else {
    // single-select mode
    if (next.has(key) && next.size === 1) {
      next.clear();
    } else {
      next.clear();
      next.add(key);
    }
  }

  setSelected(next);
  onChangeRef.current?.(next);
},
[selected, multiselect, setSelected]

);

// Precompute a stable array of row models so only affected rows update const rows = useMemo(() => { return items.map((item) => { const key = getKey(item); const isSelected = selected.has(key); return { key, item, isSelected } as const; }); }, [items, getKey, selected]);

return ( <ul role="listbox" className={"grid gap-1 " + (className ?? "")} aria-multiselectable={multiselect || undefined} > {rows.map(({ key, item, isSelected }) => ( <Row key={String(key)} item={item} selected={isSelected} onToggle={() => toggleKey(key)} renderItem={renderItem} /> ))} </ul> ); }

/**

------------------ Demo below (default export for preview) ------------------

You can delete the demo and import { SelectableList } in your app. */


type User = { id: number; name: string; role: string };

const demoItems: User[] = [ { id: 1, name: "Ada Lovelace", role: "Engineer" }, { id: 2, name: "Grace Hopper", role: "Admiral" }, { id: 3, name: "Linus Torvalds", role: "Hacker" }, ];

export default function Demo() { const [controlled, setControlled] = useState<Set<KeyLike>>(new Set([2]));

return ( <div className="p-4 max-w-xl mx-auto"> <h1 className="text-2xl font-semibold mb-3">SelectableList — Demo</h1>

<div className="mb-2 text-sm opacity-70">
    Controlled selection (click to toggle; multi-select on): {" "}
    {[...controlled].join(", ") || "∅"}
  </div>

  <SelectableList<User>
    items={demoItems}
    getKey={(u) => u.id}
    renderItem={(u) => (
      <div className="flex items-center justify-between">
        <span className="font-medium">{u.name}</span>
        <span className="text-xs opacity-70">{u.role}</span>
      </div>
    )}
    selectedKeys={controlled}
    onChange={(next) => setControlled(new Set(next))}
    multiselect
  />
</div>

); }

