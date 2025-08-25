import fs from "fs";
import path from "path";
import os from "os";
import { app, BrowserWindow, shell, ipcMain, IpcMainEvent, screen, clipboard, nativeImage } from 'electron';
import { resolveHtmlPath } from './util';
const sharp = require('sharp');
import { mainWindow } from "./main";
const axios = require('axios');
const FormData = require('form-data');
import { Monitor } from 'node-screenshots';

let screenshotPath = "";
const tempDir = os.tmpdir();
let mousePos: Electron.Point;

export const closeCaptureScreen = async () => {
  try {
    if (captureWindow) {
      captureWindow.hide();
    }
  } catch (err) {
    console.error("Error capturing screenshot:", err);
  }
};

export const captureScreen = async () => {
  try {
    // Get the current mouse position
    mousePos = screen.getCursorScreenPoint();

    if (mousePos == null) {
      throw new Error("Failed to get mouse position");
    }

    // Get all displays
    const displays = screen.getAllDisplays();

    // Find the display where the mouse cursor is located
    const targetDisplay = displays.find(display =>
      mousePos.x >= display.bounds.x &&
      mousePos.x <= display.bounds.x + display.bounds.width &&
      mousePos.y >= display.bounds.y &&
      mousePos.y <= display.bounds.y + display.bounds.height
    );

    if (!targetDisplay) {
      throw new Error("No display found for cursor position");
    }
    
    const monitor = Monitor.fromPoint(mousePos.x, mousePos.y); // Get monitor at cursor position
    if (!monitor) {
      throw new Error("No monitor found at cursor position");
    }

    const image = monitor.captureImageSync();
    screenshotPath = path.resolve(tempDir, `capyap-screenshot-temp.png`);
    fs.writeFileSync(screenshotPath, image.toPngSync());

    await createCaptureWindow();
  } catch (err) {
    console.error("Error capturing screenshot:", err);
  }
};

async function handleCropData(event: IpcMainEvent, cropData: { x: number; y: number; width: number; height: number }) {
  try {
    const outputPath = path.resolve(tempDir, `capyap-screenshot-cropped.jpg`);

    await sharp(screenshotPath)
      .extract({
        left: Math.floor(cropData.x),
        top: Math.floor(cropData.y),
        width: Math.floor(cropData.width),
        height: Math.floor(cropData.height),
      })
      .resize({ height: 2160, withoutEnlargement: true }) // Resize to a maximum width of 800px
      .jpeg({ quality: 75, })
      .toFile(outputPath);

      await uploadCroppedImage(outputPath);
  } catch (error) {
    console.error("Error cropping image:", error);
    event.reply("crop-complete", { success: false, error: error });
  }
}

const RESOURCES_PATH = app.isPackaged
  ? path.join(process.resourcesPath, 'assets')
  : path.join(__dirname, '../../assets');

const getAssetPath = (...paths: string[]): string => {
  return path.join(RESOURCES_PATH, ...paths);
};

var captureWindow: BrowserWindow | null;
var uploadPanel: BrowserWindow | null;
const uploadPanelWidth = 400, uploadPanelHeight = 100;
var captureWindowReady = false;
var uploadPanelReady = false;

export function createScreenshotWindows() {
  captureWindow = new BrowserWindow({
    show: false,
    fullscreen: true,
    frame: false,
    resizable: false,
    minimizable: false,
    maximizable: true,
    transparent: true,
    alwaysOnTop: true,
    icon: getAssetPath('icon.png'),
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      devTools: false,
      preload: app.isPackaged
        ? path.join(__dirname, 'preload.js')
        : path.join(__dirname, '../../.erb/dll/preload.js'),
    },
  });
  uploadPanel = new BrowserWindow({
    width: uploadPanelWidth,
    height: uploadPanelHeight,
    show: false,
    frame: false,
    resizable: false,
    minimizable: false,
    maximizable: true,
    alwaysOnTop: true,
    icon: getAssetPath('icon.png'),
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      devTools: false,
      preload: app.isPackaged
        ? path.join(__dirname, 'preload.js')
        : path.join(__dirname, '../../.erb/dll/preload.js'),
    },
  });
}

async function createCaptureWindow() {
  if (!captureWindow) {
    throw new Error("Capture window was null");
  }

  if (mousePos == null) {
    // Get the current mouse position
    mousePos = screen.getCursorScreenPoint();
  }

  if (mousePos == null) {
    throw new Error("Failed to get mouse position");
  }

  // Find the display nearest to the cursor
  const targetDisplay = screen.getDisplayNearestPoint(mousePos);
  
  // Set bounds
  const { x, y, width, height } = targetDisplay.bounds;

  captureWindow.setPosition(
    x,
    y
  );
  captureWindow.setSize(
    width,
    height
  );

  const indexUrl = resolveHtmlPath('index.html');
  captureWindow.loadURL(`${indexUrl}#capture`);

  const handleWindow = () => {
    if (!captureWindow) {
      throw new Error('"captureWindow" is not defined');
    }
    captureWindow.off('ready-to-show', handleWindow);

    captureWindowReady  = true;
    
    captureWindow.show();
    captureWindow.maximize();
    captureWindow.setFullScreen(true);

    const fileData = fs.readFileSync(screenshotPath).toString("base64");
    captureWindow.webContents.send("capture-file", fileData);
  }

  if (!captureWindowReady) {
    captureWindow.on('ready-to-show', handleWindow);
    ipcMain.on('crop-data', handleCropData);
  } else {
    handleWindow();
  }

  // Open urls in the user's browser
  captureWindow.webContents.setWindowOpenHandler((edata) => {
    shell.openExternal(edata.url);
    return { action: 'deny' };
  });
}

async function uploadCroppedImage(croppedPath: string) {
  if (!uploadPanel) {
    throw new Error("Upload panel was null");
  }

  try {
    if (captureWindow) {
      captureWindow.reload();
      captureWindow.hide();
    }

    if (!mainWindow) throw new Error("Main window not available");

    const primaryDisplay = screen.getPrimaryDisplay();
    uploadPanel.setPosition(
      primaryDisplay.bounds.x + primaryDisplay.bounds.width - uploadPanelWidth - 10,
      primaryDisplay.bounds.y + primaryDisplay.bounds.height - uploadPanelHeight - 10 - 47
    );

    const indexUrl = resolveHtmlPath('index.html');
    uploadPanel.loadURL(`${indexUrl}#uploading`);

    const handleWindow = () => {
      if (!uploadPanel) {
        throw new Error('"uploadPanel" is not defined');
      }

      uploadPanelReady = true;

      uploadPanel.show();
      uploadPanel.setAlwaysOnTop(true);
      uploadPanel.off('ready-to-show', handleWindow);
    }

    if (!uploadPanelReady) {
      uploadPanel.on('ready-to-show', handleWindow);
    } else {
      handleWindow();
    }

    const keys = await fetchAuthKeys(); // Step 1 is used here

    if (!keys?.uploadKey) {
      throw new Error("Missing auth keys");
    }

    const fileBuffer = fs.readFileSync(croppedPath);
    const fileName = path.basename(croppedPath);

    const formData = new FormData();
    formData.append('file', fileBuffer, { filename: fileName });

    const uploadUrl = `https://sc.marakusa.me/f/u?k=${encodeURIComponent(keys.uploadKey)}`;

    const response = await axios.post(uploadUrl, formData, {
      headers: formData.getHeaders(),
    });

    if (response.status > 299) {
      const errorMessage = response.data?.error || "Upload failed";
      const imageFile = nativeImage.createFromPath(croppedPath);
      clipboard.writeImage(imageFile);
      setTimeout(() => {
        if (uploadPanel) {
          uploadPanel.webContents.send("upload-failed", errorMessage);
        }
      }, 1000);
      setTimeout(() => {
        if (uploadPanel) {
          uploadPanel.reload();
          uploadPanel.hide();
        }
      }, 5000);
      return;
    }
    
    const capUrl = response.data.url;
    clipboard.writeText(capUrl);

    if (uploadPanel) {
      uploadPanel.reload();
      uploadPanel.hide();
    }
  } catch (err) {
    console.error("Failed to upload cropped image:", err);
    if (uploadPanel) {
      const imageFile = nativeImage.createFromPath(croppedPath);
      clipboard.writeImage(imageFile);
      setTimeout(() => {
        if (uploadPanel) {
          uploadPanel.webContents.send("upload-failed", err);
        }
      }, 1000);
      setTimeout(() => {
        if (uploadPanel) {
          uploadPanel.reload();
          uploadPanel.hide();
        }
      }, 5000);
    }
  }
}

// Fetch aw_jwt and uploadKey
async function fetchAuthKeys() {
  if (!mainWindow) return null;

  try {
    const keys = await mainWindow.webContents.executeJavaScript(`
      ({
        uploadKey: localStorage.getItem('uploadKey')
      })
    `);
    return keys;
  } catch (err) {
    console.error('Error fetching auth keys:', err);
    return null;
  }
}
