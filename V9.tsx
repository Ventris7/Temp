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
  const [resizeDirection, setResizeDirection] = useState<string | null>(null);
  const imgRef = useRef<HTMLImageElement | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  const getMousePos = (e: MouseEvent | React.MouseEvent) => {
    if (!imgRef.current || !containerRef.current) return { x: 0, y: 0 };
    const rect = imgRef.current.getBoundingClientRect();
    return {
      x: e.clientX - rect.left,
      y: e.clientY - rect.top,
    };
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    const { x, y } = getMousePos(e);
    if (!resizeDirection) {
      setCurrentArea({ x, y, width: 0, height: 0 });
      setIsDrawing(true);
    }
    e.preventDefault();
  };

  const handleMouseMove = (e: MouseEvent) => {
    const { x, y } = getMousePos(e);

    if (isDrawing && currentArea) {
      const width = x - (currentArea.x || 0);
      const height = y - (currentArea.y || 0);
      setCurrentArea({ ...currentArea, width, height });
    } else if (resizingIndex !== null && resizeDirection) {
      setAreas((prevAreas) => {
        const updatedAreas = [...prevAreas];
        const area = updatedAreas[resizingIndex];

        // Handle resizing logic for each direction
        if (resizeDirection.includes('right')) area.width = Math.max(0, x - area.x);
        if (resizeDirection.includes('left')) {
          const newX = Math.min(area.x + area.width, x);
          area.width = area.x + area.width - newX;
          area.x = newX;
        }
        if (resizeDirection.includes('bottom')) area.height = Math.max(0, y - area.y);
        if (resizeDirection.includes('top')) {
          const newY = Math.min(area.y + area.height, y);
          area.height = area.y + area.height - newY;
          area.y = newY;
        }

        return updatedAreas;
      });
    }
  };

  const handleMouseUp = () => {
    if (isDrawing && currentArea && currentArea.width && currentArea.height) {
      const newArea = {
        x: currentArea.x!,
        y: currentArea.y!,
        width: Math.abs(currentArea.width!),
        height: Math.abs(currentArea.height!),
      };
      setAreas((prev) => [...prev, newArea]);
      setCurrentArea(null);
      setIsDrawing(false);
    }
    setResizingIndex(null);
    setResizeDirection(null);
  };

  const removeArea = (index: number) => {
    setAreas(areas.filter((_, i) => i !== index));
  };

  const handleResizeStart = (index: number, direction: string) => {
    setResizingIndex(index);
    setResizeDirection(direction);
  };

  useEffect(() => {
    if (isDrawing || resizingIndex !== null) {
      const handleMouseMoveGlobal = (e: MouseEvent) => handleMouseMove(e);
      const handleMouseUpGlobal = () => handleMouseUp();

      document.addEventListener('mousemove', handleMouseMoveGlobal);
      document.addEventListener('mouseup', handleMouseUpGlobal);

      return () => {
        document.removeEventListener('mousemove', handleMouseMoveGlobal);
        document.removeEventListener('mouseup', handleMouseUpGlobal);
      };
    }
  }, [isDrawing, resizingIndex, resizeDirection]);

  return (
    <div
      ref={containerRef}
      className="image-container"
      onMouseDown={handleMouseDown}
      onMouseUp={handleMouseUp}
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
          {/* Handles for resizing */}
          <div className="resize-handle top-left" onMouseDown={(e) => handleResizeStart(index, 'top-left')} />
          <div className="resize-handle top-right" onMouseDown={(e) => handleResizeStart(index, 'top-right')} />
          <div className="resize-handle bottom-left" onMouseDown={(e) => handleResizeStart(index, 'bottom-left')} />
          <div className="resize-handle bottom-right" onMouseDown={(e) => handleResizeStart(index, 'bottom-right')} />
          <div className="resize-handle top" onMouseDown={(e) => handleResizeStart(index, 'top')} />
          <div className="resize-handle right" onMouseDown={(e) => handleResizeStart(index, 'right')} />
          <div className="resize-handle bottom" onMouseDown={(e) => handleResizeStart(index, 'bottom')} />
          <div className="resize-handle left" onMouseDown={(e) => handleResizeStart(index, 'left')} />
        </div>
      ))}
      {currentArea && (
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
