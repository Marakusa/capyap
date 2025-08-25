import { HashRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';
import { useState } from 'react';
import { CropSelector } from './CropSelector';
import Uploading from './Uploading';

function ScreenshotCapture() {
  const [currentShot, setCurrentShot] = useState<string | null>(null);

  const handleCrop = (crop: {
    x: number;
    y: number;
    width: number;
    height: number;
  }) => {
    window.electron?.ipcRenderer.sendCropData(crop);
  };

  window.electron?.ipcRenderer.onCaptureFile((dataUrl: string) => {
    setCurrentShot(dataUrl);
  });

  return (
    <div>
      {currentShot ? (
        <CropSelector imageData={currentShot} onCrop={handleCrop} />
      ) : (
        <p>No image</p>
      )}
    </div>
  );
}

export default function App() {
  return (
    <Router>
      <Routes>
        <Route path="/capture" element={<ScreenshotCapture />} />
        <Route path="/uploading" element={<Uploading />} />
      </Routes>
    </Router>
  );
}
