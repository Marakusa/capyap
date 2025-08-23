import fs from "fs";
import path from "path";
import os from "os";
import { app, BrowserWindow, shell, ipcMain, IpcMainEvent } from 'electron';
import { resolveHtmlPath } from './util';
import { mouse } from "@nut-tree-fork/nut-js";
import { Monitor } from "node-screenshots";
import sharp from "sharp";
import { mainWindow } from "./main";
const fetch = require('node-fetch');

let screenshotPath = "";
let captureWindow: BrowserWindow | null = null;
let tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'capyap-'));

export const captureScreen = async () => {
  try {
    const { x, y } = await mouse.getPosition();

    const mon = Monitor.fromPoint(x, y);
    if (!mon) {
      throw new Error("No monitor found for cursor");
    }
    
    const img = await mon.captureImage();
    screenshotPath = path.resolve(tempDir, `screenshot-temp.png`);

    fs.writeFileSync(screenshotPath, await img.toPng());

    createCaptureWindow();
  } catch (err) {
    console.error("Error capturing screenshot:", err);
  }
};

async function handleCropData(event: IpcMainEvent, cropData: { x: number; y: number; width: number; height: number }) {
  try {
    const outputPath = path.resolve(tempDir, `screenshot-cropped.png`);

    await sharp(screenshotPath)
      .extract({
        left: Math.floor(cropData.x),
        top: Math.floor(cropData.y),
        width: Math.floor(cropData.width),
        height: Math.floor(cropData.height),
      })
      .toFile(outputPath);

      await uploadCroppedImage(outputPath);
  } catch (error) {
    console.error("Error cropping image:", error);
    event.reply("crop-complete", { success: false, error: error });
  }
}

function createCaptureWindow() {
  const RESOURCES_PATH = app.isPackaged
    ? path.join(process.resourcesPath, 'assets')
    : path.join(__dirname, '../../assets');

  const getAssetPath = (...paths: string[]): string => {
    return path.join(RESOURCES_PATH, ...paths);
  };
  
  captureWindow = new BrowserWindow({
    show: false,
    //fullscreen: true,
    //frame: false,
    //resizable: false,
    //minimizable: false,
    //maximizable: true,
    transparent: true,
    //alwaysOnTop: true,
    icon: getAssetPath('icon.png'),
    webPreferences: {
      //devTools: false,
      preload: app.isPackaged
        ? path.join(__dirname, 'preload.js')
        : path.join(__dirname, '../../.erb/dll/preload.js'),
    },
  });

  const indexUrl = resolveHtmlPath('index.html');
  captureWindow.loadURL(`${indexUrl}#capture`);

  captureWindow.on('ready-to-show', () => {
    if (!captureWindow) {
      throw new Error('"captureWindow" is not defined');
    }

    captureWindow.show();
    captureWindow.maximize();
    captureWindow.setFullScreen(true);

    ipcMain.on('crop-data', handleCropData);

    const fileData = fs.readFileSync(screenshotPath).toString("base64");
    captureWindow.webContents.send("capture-file", fileData);
  });

  captureWindow.on('closed', () => {
    captureWindow = null;
  });

  // Open urls in the user's browser
  captureWindow.webContents.setWindowOpenHandler((edata) => {
    shell.openExternal(edata.url);
    return { action: 'deny' };
  });
}

async function uploadCroppedImage(croppedPath: string) {
  try {
    if (!mainWindow) throw new Error("Main window not available");

    const keys = await fetchAuthKeys(); // Step 1 is used here

    if (!keys?.aw_jwt || !keys?.uploadKey) {
      throw new Error("Missing auth keys");
    }

    const fileBuffer = fs.readFileSync(croppedPath);
    const fileName = path.basename(croppedPath);

    const FormData = require('form-data'); // Node FormData
    const formData = new FormData();
    console.log(keys.aw_jwt.value);
    console.log(keys.uploadKey);
    formData.append('file', fileBuffer, { filename: fileName });
    formData.append('sessionKey', keys.aw_jwt.value);
    formData.append('uploadKey', keys.uploadKey);

    const uploadUrl = `https://sc.marakusa.me/f/upload`;

    const response = await fetch(uploadUrl, {
      method: 'POST',
      body: formData,
    });

    if (!response.ok) {
      const errorMessage = await response.text();
      throw new Error(errorMessage);
    }

    const data = await response.json();

    if (captureWindow) {
      //captureWindow.close();
    }
  } catch (err) {
    console.error("Failed to upload cropped image:", err);
  }
}

// Fetch aw_jwt and uploadKey
async function fetchAuthKeys() {
  if (!mainWindow) return null;

  try {
    const keys = await mainWindow.webContents.executeJavaScript(`
      ({
        aw_jwt: localStorage.getItem('aw_jwt'),
        uploadKey: localStorage.getItem('uploadKey')
      })
    `);
    return keys;
  } catch (err) {
    console.error('Error fetching auth keys:', err);
    return null;
  }
}
