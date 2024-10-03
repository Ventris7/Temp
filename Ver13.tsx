import React, { useState, useRef, useEffect } from 'react';
import './ImageSelect.css';

interface Area {
  x: number;
  y: number;
  width: number;
  height: number;
}

interface ImageSelectProps {
  src: string;
}

const ImageSelect: React.FC<ImageSelectProps> = ({ src }) => {
  const [areas, setAreas] = useState<Area[]>([]);
  const [currentArea, setCurrentArea] = useState<Partial<Area> | null>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const [resizingIndex, setResizingIndex] = useState<number | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState<{ x: number; y: number } | null>(null);
  
  const imgRef = useRef<HTMLImageElement | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  const getMousePos = (e: React.MouseEvent | MouseEvent) => {
    if (!imgRef.current || !containerRef.current) return { x: 0, y: 0 };
    const rect = imgRef.current.getBoundingClientRect();
    return {
      x: e.clientX - rect.left + containerRef.current.scrollLeft,
      y: e.clientY - rect.top + containerRef.current.scrollTop,
    };
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    const { x, y } = getMousePos(e);
    setCurrentArea({ x, y });
    setIsDrawing(true);
    e.preventDefault();
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (isDrawing && currentArea) {
      const { x, y } = getMousePos(e);
      const width = x - (currentArea.x || 0);
      const height = y - (currentArea.y || 0);
      setCurrentArea({
        ...currentArea,
        width,
        height,
      });
    } else if (resizingIndex !== null) {
      const area = areas[resizingIndex];
      const { x, y } = getMousePos(e);
      const newWidth = Math.max(0, x - area.x);
      const newHeight = Math.max(0, y - area.y);
      setAreas((prev) => {
        const updatedAreas = [...prev];
        updatedAreas[resizingIndex] = { ...area, width: newWidth, height: newHeight };
        return updatedAreas;
      });
    } else if (isDragging && dragOffset) {
      const { x: offsetX, y: offsetY } = dragOffset;
      const { x, y } = getMousePos(e);
      const deltaX = x - offsetX;
      const deltaY = y - offsetY;

      // Обновляем позиции всех сегментов
      setAreas((prev) =>
        prev.map((area) => ({
          ...area,
          x: area.x + deltaX,
          y: area.y + deltaY,
        }))
      );
      setDragOffset({ x, y });
    }
  };

  const handleMouseUp = () => {
    if (currentArea && currentArea.width && currentArea.height) {
      const newArea = {
        x: Math.min(currentArea.x || 0, currentArea.x! + currentArea.width!),
        y: Math.min(currentArea.y || 0, currentArea.y! + currentArea.height!),
        width: Math.abs(currentArea.width!),
        height: Math.abs(currentArea.height!),
      };

      if (!checkIntersection(newArea)) {
        setAreas((prevAreas) => [...prevAreas, newArea]);
      }
    }
    setCurrentArea(null);
    setIsDrawing(false);
    setResizingIndex(null);
    setIsDragging(false);
    setDragOffset(null);
  };

  const checkIntersection = (newArea: Area): boolean => {
    return areas.some((area) => {
      return !(
        newArea.x + newArea.width < area.x ||
        newArea.x > area.x + area.width ||
        newArea.y + newArea.height < area.y ||
        newArea.y > area.y + area.height
      );
    });
  };

  const removeArea = (index: number) => {
    setAreas(areas.filter((_, i) => i !== index));
  };

  const handleWheel = (e: React.WheelEvent) => {
    if (containerRef.current) {
      containerRef.current.scrollLeft += e.deltaY;
    }
  };

  const handleDragStart = (e: React.DragEvent) => {
    e.preventDefault();
  };

  // Обработчик для изменения размеров
  const handleMouseDownResize = (index: number) => {
    setResizingIndex(index);
  };

  // Обработчик для перемещения при зажатом среднем колесе мыши
  const handleMouseDownMiddle = (e: React.MouseEvent) => {
    if (e.button === 1) { // Среднее колесо мыши
      const { x, y } = getMousePos(e);
      setIsDragging(true);
      setDragOffset({ x, y });
      e.preventDefault();
    }
  };

  useEffect(() => {
    if (isDrawing || resizingIndex !== null || isDragging) {
      const handleMouseMoveGlobal = (e: MouseEvent) => handleMouseMove(e);
      const handleMouseUpGlobal = () => handleMouseUp();

      document.addEventListener('mousemove', handleMouseMoveGlobal);
      document.addEventListener('mouseup', handleMouseUpGlobal);

      return () => {
        document.removeEventListener('mousemove', handleMouseMoveGlobal);
        document.removeEventListener('mouseup', handleMouseUpGlobal);
      };
    }
  }, [isDrawing, currentArea, resizingIndex, isDragging]);

  return (
    <div
      ref={containerRef}
      className="image-container"
      onMouseDown={handleMouseDown}
      onWheel={handleWheel}
      onDragStart={handleDragStart}
      onMouseDown={handleMouseDownMiddle} // Добавлено для обработки нажатия на среднее колесо
    >
      <img ref={imgRef} src={src} alt="Selectable" draggable={false} />
      {areas.map((area, index) => (
        <div
          key={index}
          className="area"
          style={{
            left: area.x,
            top: area.y,
            width: area.width,
            height: area.height,
          }}
        >
          <button className="remove-btn" onClick={() => removeArea(index)}>
            ×
          </button>
          <div
            className="resize-handle"
            onMouseDown={() => handleMouseDownResize(index)}
            style={{
              cursor: 'nwse-resize',
            }}
          />
        </div>
      ))}
      {isDrawing && currentArea && (
        <div
          className="current-area"
          style={{
            left: currentArea.x,
            top: currentArea.y,
            width: currentArea.width,
            height: currentArea.height,
          }}
        />
      )}
    </div>
  );
};

export default ImageSelect;
