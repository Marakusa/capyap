import { HashRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';
import { useState } from "react";
import { CropSelector } from "./CropSelector";

function Hello() {
  return (
    <div>
      <h1>CapYap</h1>
      <p>Start by signing in.</p>
      <div className="Hello">
        <a
          href="https://electron-react-boilerplate.js.org/"
          target="_blank"
          rel="noreferrer"
        >
          <button type="button">
            <span role="img" aria-label="books">
              📚
            </span>
            Log In with Discord
          </button>
        </a>
      </div>
    </div>
  );
}

function ScreenshotCapture() {
  const [currentShot, setCurrentShot] = useState<string | null>(null);

  const handleCrop = (crop: { x: number; y: number; width: number; height: number }) => {
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
        <Route path="/" element={<Hello />} />
        <Route path="/capture" element={<ScreenshotCapture />} />
      </Routes>
    </Router>
  );
}
