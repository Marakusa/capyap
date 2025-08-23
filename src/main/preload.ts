// Disable no-unused-vars, broken for spread args
/* eslint no-unused-vars: off */
import { contextBridge, ipcRenderer, IpcRendererEvent } from 'electron';
import fs from 'fs';

export type Channels = 'ipc-example';

const electronHandler = {
  ipcRenderer: {
    sendCropData: (cropData: { x: number; y: number; width: number; height: number }) => ipcRenderer.send('crop-data', cropData),
    sendCaptureFile: (base64Data: string) => ipcRenderer.send('capture-file', base64Data),
    onCaptureFile: (callback: (base64Data: string) => void) => {
      ipcRenderer.on('capture-file', (_, base64Data) => callback(`data:image/png;base64,${base64Data}`));
    },
  },
};

contextBridge.exposeInMainWorld('electron', electronHandler);

export type ElectronHandler = typeof electronHandler;
