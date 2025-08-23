import fs from "fs";
import path from "path";
import os from "os";
import { app, BrowserWindow, shell, ipcMain, IpcMainEvent } from 'electron';
import { resolveHtmlPath } from './util';
import { mouse } from "@nut-tree-fork/nut-js";
import { Monitor } from "node-screenshots";
import sharp from "sharp";

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
    console.log(`Saved screenshot: ${screenshotPath}`);

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

    console.log(`Cropped screenshot saved to: ${outputPath}`);

    if (captureWindow) {
      captureWindow.close();
    }
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
    fullscreen: true,
    frame: false,
    transparent: true,
    alwaysOnTop: true,
    icon: getAssetPath('icon.png'),
    webPreferences: {
      devTools: false,
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
