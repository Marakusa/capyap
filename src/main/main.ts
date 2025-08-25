import path from 'path';
import { app, BrowserWindow, shell, globalShortcut } from 'electron';
import MenuBuilder from './menu';
import { captureScreen, closeCaptureScreen, createScreenshotWindows } from "./screenshot";

export let mainWindow: BrowserWindow | null = null;

if (process.env.NODE_ENV === 'production') {
  const sourceMapSupport = require('source-map-support');
  sourceMapSupport.install();
}

const isDebug =
  process.env.NODE_ENV === 'development' || process.env.DEBUG_PROD === 'true';

if (isDebug) {
  require('electron-debug').default();
}

const createWindow = async () => {
  const RESOURCES_PATH = app.isPackaged
    ? path.join(process.resourcesPath, 'assets')
    : path.join(__dirname, '../../assets');

  const getAssetPath = (...paths: string[]): string => {
    return path.join(RESOURCES_PATH, ...paths);
  };

  mainWindow = new BrowserWindow({
    show: false,
    width: 1480,
    height: 900,
    icon: getAssetPath('icon.png'),
    webPreferences: {
      devTools: false,
      nodeIntegration: false,
      contextIsolation: true,
      preload: app.isPackaged
        ? path.join(__dirname, 'preload.js')
        : path.join(__dirname, '../../.erb/dll/preload.js'),
    },
  });

  //mainWindow.loadURL(resolveHtmlPath('index'));
  mainWindow.loadURL('https://sc.marakusa.me');

  mainWindow.on('ready-to-show', () => {
    if (!mainWindow) {
      throw new Error('"mainWindow" is not defined');
    }
    
    mainWindow.setMenu(null);

    if (process.env.START_MINIMIZED) {
      mainWindow.minimize();
    } else {
      mainWindow.show();
    }
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });

  const menuBuilder = new MenuBuilder(mainWindow);
  menuBuilder.buildMenu();

  // Open urls in the user's browser
  mainWindow.webContents.setWindowOpenHandler((edata) => {
    shell.openExternal(edata.url);
    return { action: 'deny' };
  });
};

/**
 * Add event listeners...
 */

app.on('window-all-closed', () => {
  // Respect the OSX convention of having the application in memory even
  // after all windows have been closed
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app
  .whenReady()
  .then(() => {
    createWindow();
    createScreenshotWindows();

    // Register Global Shortcut
    globalShortcut.register('CommandOrControl+PrintScreen', () => {
      captureScreen();
    });
    globalShortcut.register('Escape', () => {
      closeCaptureScreen();
    });

    app.on('activate', () => {
      if (mainWindow === null) createWindow();
    });
  })
  .catch(console.log);

// Clean up shortcuts
app.on('will-quit', () => {
  globalShortcut.unregisterAll();
});
