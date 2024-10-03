import React, { useState, useRef, useEffect } from 'react';
import './ImageAreasSelector.css';

interface Area {
  id: number;
  x: number;
  y: number;
  width: number;
  height: number;
  originalX: number;
  originalY: number;
  originalWidth: number;
  originalHeight: number;
}

const ImageAreasSelector: React.FC = () => {
  const [areas, setAreas] = useState<Area[]>([]);
  const [currentArea, setCurrentArea] = useState<Area | null>(null);
  const [isPanning, setIsPanning] = useState(false);
  const [scale, setScale] = useState(1);
  const [imagePosition, setImagePosition] = useState({ x: 0, y: 0 });
  const [isResizing, setIsResizing] = useState<Area | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const imageRef = useRef<HTMLImageElement>(null);
  const startPositionRef = useRef<{ x: number; y: number } | null>(null);
  const startSizeRef = useRef<{ width: number; height: number } | null>(null);

  // Обработчик прокрутки мыши для зума
  const handleMouseWheel = (event: React.WheelEvent) => {
    event.preventDefault();
    const zoomFactor = 0.1;
    let newScale = scale;

    // Увеличение или уменьшение масштаба
    if (event.deltaY < 0) {
      newScale = scale + zoomFactor;
    } else {
      newScale = scale - zoomFactor;
    }

    if (newScale < 0.5) newScale = 0.5;
    if (newScale > 3) newScale = 3;

    const container = containerRef.current!;
    const rect = container.getBoundingClientRect();
    const offsetX = (event.clientX - rect.left) / rect.width;
    const offsetY = (event.clientY - rect.top) / rect.height;

    // Перемещение изображения при зуме
    setImagePosition((prev) => ({
      x: prev.x - offsetX * (newScale - scale) * rect.width,
      y: prev.y - offsetY * (newScale - scale) * rect.height,
    }));

    setScale(newScale);
  };

  // Обработчик нажатия мыши
  const handleMouseDown = (event: React.MouseEvent) => {
    if (event.button === 1) {
      event.preventDefault();
      setIsPanning(true);
    }
  };

  // Обработчик движения мыши
  const handleMouseMove = (event: React.MouseEvent) => {
    if (isPanning) {
      const { movementX, movementY } = event;
      setImagePosition((prev) => ({
        x: prev.x + movementX,
        y: prev.y + movementY,
      }));
    } else if (isResizing) {
      if (currentArea) {
        const newWidth = Math.max(10, startSizeRef.current!.width + (event.clientX - startPositionRef.current!.x) / scale);
        const newHeight = Math.max(10, startSizeRef.current!.height + (event.clientY - startPositionRef.current!.y) / scale);
        setAreas((prev) =>
          prev.map(area =>
            area.id === currentArea.id
              ? { ...area, width: newWidth, height: newHeight }
              : area
          )
        );
      }
    }
  };

  // Обработчик отпускания кнопки мыши
  const handleMouseUp = (event: React.MouseEvent) => {
    if (event.button === 1) {
      setIsPanning(false);
    } else if (isResizing) {
      setIsResizing(null);
      setCurrentArea(null);
    }
  };

  // Обработчик клика по области
  const handleAreaClick = (event: React.MouseEvent, area: Area) => {
    event.stopPropagation();
    setCurrentArea(area);
    startPositionRef.current = { x: event.clientX, y: event.clientY };
    startSizeRef.current = { width: area.width, height: area.height };
    setIsResizing(area);
  };

  // Функция создания новой области
  const handleAreaCreation = (x: number, y: number, width: number, height: number) => {
    const newArea: Area = {
      id: areas.length + 1,
      x: x / scale,
      y: y / scale,
      width: width / scale,
      height: height / scale,
      originalX: x,
      originalY: y,
      originalWidth: width,
      originalHeight: height,
    };
    setAreas([...areas, newArea]);
  };

  // Функция удаления области
  const handleDeleteArea = (id: number) => {
    setAreas(areas.filter(area => area.id !== id));
  };

  // Обработчик клика на изображении для добавления области
  const handleMouseDownOnImage = (event: React.MouseEvent) => {
    if (event.button === 0) {
      const rect = imageRef.current!.getBoundingClientRect();
      const x = (event.clientX - rect.left - imagePosition.x) * scale;
      const y = (event.clientY - rect.top - imagePosition.y) * scale;
      handleAreaCreation(x, y, 100, 100); // Пример с фиксированными размерами
    }
  };

  useEffect(() => {
    const handleMouseWheelListener = (e: WheelEvent) => e.preventDefault();
    window.addEventListener('wheel', handleMouseWheelListener, { passive: false });

    return () => {
      window.removeEventListener('wheel', handleMouseWheelListener);
    };
  }, []);

  return (
    <div
      className="image-container"
      ref={containerRef}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseWheel={handleMouseWheel}
      onMouseDown={handleMouseDown}
    >
      <img
        ref={imageRef}
        src="/path-to-your-image.jpg" // Замените на путь к вашему изображению
        alt="Selectable"
        className="image"
        style={{
          transform: `translate(${imagePosition.x}px, ${imagePosition.y}px) scale(${scale})`,
        }}
        onMouseDown={handleMouseDownOnImage}
        draggable={false} // Отключение перетаскивания по умолчанию
      />
      {areas.map(area => (
        <div
          key={area.id}
          className="area"
          style={{
            left: area.x * scale,
            top: area.y * scale,
            width: area.width * scale,
            height: area.height * scale,
          }}
          onMouseDown={(event) => handleAreaClick(event, area)}
        >
          <div className="delete-btn" onClick={() => handleDeleteArea(area.id)}>x</div>
        </div>
      ))}
    </div>
  );
};

export default ImageAreasSelector;
