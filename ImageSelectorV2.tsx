import React, { useState, useRef, useEffect } from 'react';
import './ImageSelect.css'; // Подключаем стили

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

  const getMousePos = (e: React.MouseEvent) => {
    if (!imgRef.current) return { x: 0, y: 0 };
    const rect = imgRef.current.getBoundingClientRect();
    return {
      x: e.clientX - rect.left,
      y: e.clientY - rect.top,
    };
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    const { x, y } = getMousePos(e);
    setCurrentArea({ x, y });
    setIsDrawing(true);
  };

  const handleMouseMove = (e: React.MouseEvent) => {
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

  useEffect(() => {
    console.log("Selected areas: ", areas);
  }, [areas]);

  return (
    <div
      style={{
        position: 'relative',
        display: 'inline-block',
        cursor: isDrawing ? 'crosshair' : 'default',
      }}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
    >
      <img ref={imgRef} src={src} alt="Selectable" style={{ display: 'block' }} />
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
