const handleMouseMove = (e: MouseEvent) => {
    const { x, y } = getMousePos(e);

    // Если идет рисование новой области
    if (isDrawing && currentArea) {
        const width = x - (currentArea.x || 0);
        const height = y - (currentArea.y || 0);
        setCurrentArea({ ...currentArea, width, height });
        return; // Прерываем выполнение, чтобы не было конфликта с resize
    }

    // Если идет изменение размеров существующей области
    if (resizingIndex !== null && resizeDirection) {
        setAreas((prevAreas) => {
            const updatedAreas = [...prevAreas];
            const area = updatedAreas[resizingIndex];

            // Обработка изменения размеров в зависимости от направления
            if (resizeDirection.includes('right')) {
                area.width = Math.max(0, x - area.x);
            }
            if (resizeDirection.includes('left')) {
                const newX = Math.min(area.x + area.width, x);
                area.width = area.x + area.width - newX;
                area.x = newX;
            }
            if (resizeDirection.includes('bottom')) {
                area.height = Math.max(0, y - area.y);
            }
            if (resizeDirection.includes('top')) {
                const newY = Math.min(area.y + area.height, y);
                area.height = area.y + area.height - newY;
                area.y = newY;
            }

            return updatedAreas;
        });
        return; // Прерываем выполнение, чтобы не было конфликтов с рисованием
    }
};
