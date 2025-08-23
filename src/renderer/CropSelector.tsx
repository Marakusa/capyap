import React, { useState, useRef } from "react";

interface CropSelectorProps {
  imageData: string; // base64 data URL
  onCrop: (crop: { x: number; y: number; width: number; height: number }) => void;
}

export const CropSelector: React.FC<CropSelectorProps> = ({
  imageData,
  onCrop,
}) => {
  const [dragging, setDragging] = useState(false);
  const [start, setStart] = useState({ x: 0, y: 0 });
  const [end, setEnd] = useState({ x: 0, y: 0 });
  const containerRef = useRef<HTMLDivElement>(null);

  const handleMouseDown = (e: React.MouseEvent) => {
    const rect = containerRef.current!.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    setStart({ x, y });
    setEnd({ x, y });
    setDragging(true);
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!dragging) return;
    const rect = containerRef.current!.getBoundingClientRect();
    setEnd({
      x: e.clientX - rect.left,
      y: e.clientY - rect.top,
    });
  };

  const handleMouseUp = () => {
    setDragging(false);
    const crop = {
      x: Math.min(start.x, end.x),
      y: Math.min(start.y, end.y),
      width: Math.abs(end.x - start.x),
      height: Math.abs(end.y - start.y),
    };
    onCrop(crop);
  };

  // Crop box dimensions
  const x = Math.min(start.x, end.x);
  const y = Math.min(start.y, end.y);
  const width = Math.abs(end.x - start.x);
  const height = Math.abs(end.y - start.y);

  return (
    <div
      ref={containerRef}
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        width: "100%",
        height: "100vh",
        cursor: "crosshair",
        background: `url(${imageData}) center / contain no-repeat`,
      }}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
    >
      {/* Overlay */}
      <svg
        width="100%"
        height="100%"
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          pointerEvents: "none",
        }}
      >
        <defs>
          <mask id="mask">
            {/* Full white background (visible) */}
            <rect width="100%" height="100%" fill="white" />
            {/* Cutout area (black = transparent) */}
            <rect x={x} y={y} width={width} height={height} fill="black" />
          </mask>
        </defs>

        {/* Dark overlay with mask */}
        <rect
          width="100%"
          height="100%"
          fill="rgba(0,0,0,0.6)"
          mask="url(#mask)"
        />
      </svg>
    </div>
  );
};
