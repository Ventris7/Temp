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
  const [currentArea, setCurrentArea] = useState<Partial<Area> | null>(null);
  const [isPanning, setIsPanning] = useState(false);
  const [scale, setScale] = useState(1);
  const [imagePosition, setImagePosition] = useState({ x: 0, y: 0 });
  const containerRef = useRef<HTMLDivElement>(null);
  const imageRef = useRef<HTMLImageElement>(null);

  const handleMouseWheel = (event: React.WheelEvent) => {
    event.preventDefault();
    const zoomFactor = 0.1;
    let newScale = scale;

    // Zooming with the mouse wheel
    if (event.deltaY < 0) {
      newScale = scale + zoomFactor;
    } else {
      newScale = scale - zoomFactor;
    }

    // Set minimum and maximum zoom levels
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
    }
  };

  const handleMouseUp = (event: React.MouseEvent) => {
    if (event.button === 1) {
      setIsPanning(false);
    }
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
        >
          <div className="delete-btn" onClick={() => handleDeleteArea(area.id)}>x</div>
        </div>
      ))}
    </div>
  );
};

export default ImageAreasSelector;
