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
  const imgRef = useRef<HTMLImageElement | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Получение позиции мыши с учётом прокрутки
  const getMousePos = (e: React.MouseEvent | MouseEvent) => {
    if (!imgRef.current || !containerRef.current) return { x: 0, y: 0 };
    const rect = imgRef.current.getBoundingClientRect();
    return {
      x: e.clientX - rect.left + containerRef.current.scrollLeft,
      y: e.clientY - rect.top + containerRef.current.scrollTop,
    };
  };

  // Начало выделения новой области
  const handleMouseDown = (e: React.MouseEvent) => {
    const { x, y } = getMousePos(e);
    setCurrentArea({ x, y });
    setIsDrawing(true);
  };

  // Обновление текущей области при движении мыши
  const handleMouseMove = (e: MouseEvent) => {
    if (!isDrawing || !currentArea) return;
    const { x, y } = getMousePos(e);
    const width = x - (currentArea.x || 0);
    const height = y - (currentArea.y || 0);
    setCurrentArea({
      ...currentArea,
      width,
      height,
    });
  };

  // Завершение выделения области
  const handleMouseUp = () => {
    if (currentArea && currentArea.width && currentArea.height) {
      const newArea = {
        x: Math.min(currentArea.x || 0, currentArea.x! + currentArea.width!),
        y: Math.min(currentArea.y || 0, currentArea.y! + currentArea.height!),
        width: Math.abs(currentArea.width!),
        height: Math.abs(currentArea.height!),
      };

      if (!checkIntersection(newArea)) {
        setAreas((prevAreas) => [...prevAreas, newArea]); // Добавляем новый сегмент в массив
      }
    }
    setCurrentArea(null);
    setIsDrawing(false);
  };

  // Проверка пересечений с уже существующими областями
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

  // Удаление области
  const removeArea = (index: number) => {
    setAreas(areas.filter((_, i) => i !== index));
  };

  // Обработчик для прокрутки с помощью колеса мышки
  const handleWheel = (e: React.WheelEvent) => {
    if (containerRef.current) {
      containerRef.current.scrollLeft += e.deltaY; // Прокручиваем по горизонтали
    }
  };

  // Добавляем глобальные обработчики для mousemove и mouseup
  useEffect(() => {
    if (isDrawing) {
      const handleMouseMoveGlobal = (e: MouseEvent) => handleMouseMove(e);
      const handleMouseUpGlobal = () => handleMouseUp();

      document.addEventListener('mousemove', handleMouseMoveGlobal);
      document.addEventListener('mouseup', handleMouseUpGlobal);

      return () => {
        document.removeEventListener('mousemove', handleMouseMoveGlobal);
        document.removeEventListener('mouseup', handleMouseUpGlobal);
      };
    }
  }, [isDrawing, currentArea]);

  return (
    <div
      ref={containerRef}
      className="image-container"
      onMouseDown={handleMouseDown}
      onWheel={handleWheel}
    >
      <img ref={imgRef} src={src} alt="Selectable" />
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
