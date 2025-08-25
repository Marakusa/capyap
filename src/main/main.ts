import path from 'path';
import { app, BrowserWindow, shell, globalShortcut } from 'electron';
import MenuBuilder from './menu';
import {
  captureScreen,
  closeCaptureScreen,
  createScreenshotWindows,
} from './screenshot';

const sourceMapSupport = require('source-map-support');
const electronDebug = require('electron-debug');

const Config = require('electron-config');

const config = new Config();

if (process.env.NODE_ENV === 'production') {
  sourceMapSupport.install();
}

const isDebug =
  process.env.NODE_ENV === 'development' || process.env.DEBUG_PROD === 'true';

if (isDebug) {
  electronDebug.default();
}

const RESOURCES_PATH = app.isPackaged
  ? path.join(process.resourcesPath, 'assets')
  : path.join(__dirname, '../../assets');

const getAssetPath = (...paths: string[]): string => {
  return path.join(RESOURCES_PATH, ...paths);
};

let mainWindow: BrowserWindow | null;

const createWindow = async () => {
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

  if (config.get('winBounds')) {
    mainWindow.setBounds(config.get('winBounds'));
  }
  if (config.get('maximized')) {
    mainWindow.maximize();
  }

  // mainWindow.loadURL(resolveHtmlPath('index'));
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

  mainWindow.on('moved', () => {
    if (mainWindow) {
      config.set('winBounds', mainWindow.getBounds());
      config.set('maximized', mainWindow.isMaximized());
    }
  });
  mainWindow.on('resized', () => {
    if (mainWindow) {
      config.set('winBounds', mainWindow.getBounds());
      config.set('maximized', mainWindow.isMaximized());
    }
  });
  mainWindow.on('maximize', () => {
    if (mainWindow) {
      config.set('winBounds', mainWindow.getBounds());
      config.set('maximized', mainWindow.isMaximized());
    }
  });

  mainWindow.on('closed', () => {
    if (process.platform !== 'darwin') {
      app.quit();
    }
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

// Fetch uploadKey
export async function fetchAuthKeys(): Promise<string> {
  if (!mainWindow) {
    throw new Error('"mainWindow" is not defined');
  }

  try {
    const keys = await mainWindow.webContents.executeJavaScript(`
      ({
        uploadKey: localStorage.getItem('uploadKey')
      })
    `);
    return keys.uploadKey;
  } catch (err) {
    throw new Error(`Error fetching auth keys: ${err}`);
  }
}

app.on('ready', () => {
  try {
    createWindow();
    createScreenshotWindows();

    // Register Global Shortcut
    globalShortcut.register('CommandOrControl+PrintScreen', async () => {
      captureScreen(await fetchAuthKeys());
    });
    globalShortcut.register('Escape', () => {
      closeCaptureScreen();
    });

    app.on('activate', () => {
      if (mainWindow === null) createWindow();
    });
  } catch (error) {
    throw new Error(`Failed to start the app: ${error}`);
  }
});

// Clean up shortcuts
app.on('will-quit', () => {
  globalShortcut.unregisterAll();
});
