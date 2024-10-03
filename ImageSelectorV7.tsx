import React, { useReducer, useState, useRef } from 'react';

// Константы для действий
const ACTIONS = {
  INCREMENT: 'increment',
  DECREMENT: 'decrement',
  RESET: 'reset',
  SET_COUNT: 'set_count',
  DOUBLE: 'double',
};

// Action creators
const increment = () => ({ type: ACTIONS.INCREMENT });
const decrement = () => ({ type: ACTIONS.DECREMENT });
const reset = () => ({ type: ACTIONS.RESET });
const setCount = (count: number) => ({ type: ACTIONS.SET_COUNT, payload: count });
const double = () => ({ type: ACTIONS.DOUBLE });

const initialState = { count: 0 };

// Объект с обработчиками для действий
const actionHandlers = {
  [ACTIONS.INCREMENT]: (state: { count: number }) => ({ count: state.count + 1 }),
  [ACTIONS.DECREMENT]: (state: { count: number }) => ({ count: state.count - 1 }),
  [ACTIONS.RESET]: () => ({ count: 0 }),
  [ACTIONS.SET_COUNT]: (state: { count: number }, action: { payload: number }) => ({ count: action.payload }),
  [ACTIONS.DOUBLE]: (state: { count: number }) => ({ count: state.count * 2 }),
};

// Редьюсер для обработки действий
function reducer(state: { count: number }, action: { type: string; payload?: any }) {
  const handler = actionHandlers[action.type];
  return handler ? handler(state, action) : state;
}

function ResizableSegment({ initialWidth }: { initialWidth: number }) {
  const [width, setWidth] = useState(initialWidth);
  const segmentRef = useRef<HTMLDivElement | null>(null);
  const isResizing = useRef(false);

  const handleMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    isResizing.current = true;
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (isResizing.current && segmentRef.current) {
      const newWidth = e.clientX - segmentRef.current.getBoundingClientRect().left;
      setWidth(newWidth > 50 ? newWidth : 50); // Минимальная ширина 50px
    }
  };

  const handleMouseUp = () => {
    isResizing.current = false;
    document.removeEventListener('mousemove', handleMouseMove);
    document.removeEventListener('mouseup', handleMouseUp);
  };

  return (
    <div
      ref={segmentRef}
      style={{
        width: `${width}px`,
        height: '100px',
        backgroundColor: '#d3d3d3',
        position: 'relative',
        display: 'inline-block',
      }}
    >
      <div
        style={{
          position: 'absolute',
          right: 0,
          top: 0,
          bottom: 0,
          width: '10px',
          cursor: 'col-resize',
        }}
        onMouseDown={handleMouseDown}
      />
    </div>
  );
}

function Counter() {
  const [state, dispatch] = useReducer(reducer, initialState);

  return (
    <div style={{ textAlign: 'center', marginTop: '50px' }}>
      <h1>Count: {state.count}</h1>
      <div>
        <button onClick={() => dispatch(increment())}>+</button>
        <button onClick={() => dispatch(decrement())}>-</button>
        <button onClick={() => dispatch(double())}>x2</button>
        <button onClick={() => dispatch(reset())}>Reset</button>
      </div>
      <div style={{ marginTop: '20px' }}>
        <button onClick={() => dispatch(setCount(10))}>Set Count to 10</button>
      </div>
      <div style={{ marginTop: '20px', display: 'flex', justifyContent: 'center' }}>
        <ResizableSegment initialWidth={150} />
        <ResizableSegment initialWidth={200} />
      </div>
    </div>
  );
}

export default Counter;
