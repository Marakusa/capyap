import fs from 'fs';
import path from 'path';
import os from 'os';
import {
  app,
  BrowserWindow,
  desktopCapturer,
  shell,
  ipcMain,
  IpcMainEvent,
  screen,
  clipboard,
  nativeImage,
  Point,
} from 'electron';
import { Monitor } from 'node-screenshots';
import { resolveHtmlPath } from './util';

const sharp = require('sharp');
const axios = require('axios');
const FormData = require('form-data');

let screenshotPath = '';
const tempDir = os.tmpdir();
let mousePos: Point;

let captureWindow: BrowserWindow | null;
let uploadPanel: BrowserWindow | null;
const uploadPanelWidth = 400;
const uploadPanelHeight = 100;
let captureWindowReady = false;
let uploadPanelReady = false;
let uploadKey: string | null;

export const closeCaptureScreen = async () => {
  try {
    if (captureWindow) {
      captureWindow.hide();
    }
  } catch (err) {
    throw new Error(`Error capturing screenshot: ${err}`);
  }
};

async function uploadCroppedImage(croppedPath: string) {
  if (!uploadPanel) {
    throw new Error('Upload panel was null');
  }

  try {
    if (captureWindow) {
      captureWindow.reload();
      captureWindow.hide();
    }

    const primaryDisplay = screen.getPrimaryDisplay();
    uploadPanel.setPosition(
      primaryDisplay.bounds.x +
        primaryDisplay.bounds.width -
        uploadPanelWidth -
        10,
      primaryDisplay.bounds.y +
        primaryDisplay.bounds.height -
        uploadPanelHeight -
        10 -
        47,
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
    };

    if (!uploadPanelReady) {
      uploadPanel.on('ready-to-show', handleWindow);
    } else {
      handleWindow();
    }

    if (!uploadKey) {
      throw new Error('Missing auth keys');
    }

    const fileBuffer = fs.readFileSync(croppedPath);
    const fileName = path.basename(croppedPath);

    const formData = new FormData();
    formData.append('file', fileBuffer, { filename: fileName });

    const uploadUrl = `https://sc.marakusa.me/f/u?k=${encodeURIComponent(uploadKey)}`;

    const response = await axios.post(uploadUrl, formData, {
      headers: formData.getHeaders(),
    });

    if (response.status > 299) {
      const errorMessage = response.data?.error || 'Upload failed';
      const imageFile = nativeImage.createFromPath(croppedPath);
      clipboard.writeImage(imageFile);
      setTimeout(() => {
        if (uploadPanel) {
          uploadPanel.webContents.send('upload-failed', errorMessage);
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
    if (uploadPanel) {
      const imageFile = nativeImage.createFromPath(croppedPath);
      clipboard.writeImage(imageFile);
      setTimeout(() => {
        if (uploadPanel) {
          uploadPanel.webContents.send('upload-failed', err);
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

async function handleCropData(
  event: IpcMainEvent,
  cropData: { x: number; y: number; width: number; height: number },
) {
  try {
    const outputPath = path.resolve(tempDir, `capyap-screenshot-cropped.jpg`);

    await sharp(screenshotPath)
      .extract({
        left: Math.floor(cropData.x),
        top: Math.floor(cropData.y),
        width: Math.floor(cropData.width),
        height: Math.floor(cropData.height),
      })
      .resize({ height: 2160, withoutEnlargement: true })
      .jpeg({ quality: 75 })
      .toFile(outputPath);

    await uploadCroppedImage(outputPath);
  } catch (error) {
    event.reply('crop-complete', { success: false, error });
  }
}

async function createCaptureWindow() {
  if (!captureWindow) {
    throw new Error('Capture window was null');
  }

  if (mousePos == null) {
    // Get the current mouse position
    mousePos = screen.getCursorScreenPoint();
  }

  if (mousePos == null) {
    throw new Error('Failed to get mouse position');
  }

  // Find the display nearest to the cursor
  const targetDisplay = screen.getDisplayNearestPoint(mousePos);

  // Set bounds
  const { x, y, width, height } = targetDisplay.bounds;

  captureWindow.setPosition(x, y);
  captureWindow.setSize(width, height);

  const indexUrl = resolveHtmlPath('index.html');
  captureWindow.loadURL(`${indexUrl}#capture`);

  const handleWindow = () => {
    if (!captureWindow) {
      throw new Error('"captureWindow" is not defined');
    }
    captureWindow.off('ready-to-show', handleWindow);

    captureWindowReady = true;

    captureWindow.show();
    captureWindow.maximize();
    captureWindow.setFullScreen(true);

    const fileData = fs.readFileSync(screenshotPath).toString('base64');
    captureWindow.webContents.send('capture-file', fileData);
  };

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

export const captureScreen = async (authKey: string) => {
  try {
    uploadKey = authKey;

    // Get the current mouse position
    mousePos = screen.getCursorScreenPoint();

    if (mousePos == null) {
      throw new Error('Failed to get mouse position');
    }

    const sources = await desktopCapturer.getSources({ types: ['screen'] });

      console.log(sources);
    sources.forEach((src, i) => {
      const dataUrl = src.thumbnail.toDataURL();
      const base64Data = dataUrl.replace(/^data:image\/png;base64,/, '');
      const filePath = path.join(tempDir, `capyap-screenshot-temp-${i}.png`);

      console.log(filePath);
      fs.writeFileSync(filePath, base64Data, 'base64');
    });

    sources.map((src, i) => ({
      id: src.id,
      name: src.name,
      path: path.join(tempDir, `capyap-screenshot-temp.png`),
    }));

    await createCaptureWindow();
  } catch (err) {
    throw new Error(`Error capturing screenshot: ${err}`);
  }
};

const RESOURCES_PATH = app.isPackaged
  ? path.join(process.resourcesPath, 'assets')
  : path.join(__dirname, '../../assets');

const getAssetPath = (...paths: string[]): string => {
  return path.join(RESOURCES_PATH, ...paths);
};

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
