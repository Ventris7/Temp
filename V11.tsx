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

  const handleMouseWheel = (event: React.WheelEvent) => {
    event.preventDefault();
    const zoomFactor = 0.1;
    let newScale = scale;

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

    setImagePosition((prev) => ({
      x: prev.x - offsetX * (newScale - scale) * rect.width,
      y: prev.y - offsetY * (newScale - scale) * rect.height,
    }));

    setScale(newScale);
  };

  const handleMouseDown = (event: React.MouseEvent) => {
    if (event.button === 1) {
      event.preventDefault();
      setIsPanning(true);
    }
  };

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

  const handleMouseUp = (event: React.MouseEvent) => {
    if (event.button === 1) {
      setIsPanning(false);
    } else if (isResizing) {
      setIsResizing(null);
      setCurrentArea(null);
    }
  };

  const handleAreaClick = (event: React.MouseEvent, area: Area) => {
    event.stopPropagation();
    setCurrentArea(area);
    startPositionRef.current = { x: event.clientX, y: event.clientY };
    startSizeRef.current = { width: area.width, height: area.height };
    setIsResizing(area);
  };

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

  const handleDeleteArea = (id: number) => {
    setAreas(areas.filter(area => area.id !== id));
  };

  const handleMouseDownOnImage = (event: React.MouseEvent) => {
    if (event.button === 0) {
      const rect = imageRef.current!.getBoundingClientRect();
      const x = event.clientX - rect.left - imagePosition.x;
      const y = event.clientY - rect.top - imagePosition.y;
      handleAreaCreation(x * scale, y * scale, 100, 100); // Пример с фиксированными размерами
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
        src="/path-to-your-image.jpg"
        alt="Selectable"
        className="image"
        style={{
          transform: `translate(${imagePosition.x}px, ${imagePosition.y}px) scale(${scale})`,
        }}
        onMouseDown={handleMouseDownOnImage}
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
