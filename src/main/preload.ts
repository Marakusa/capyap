// Disable no-unused-vars, broken for spread args
/* eslint no-unused-vars: off */
import { contextBridge, ipcRenderer } from 'electron';

export type Channels = 'ipc-example';

const electronHandler = {
  ipcRenderer: {
    sendCropData: (cropData: { x: number; y: number; width: number; height: number }) => ipcRenderer.send('crop-data', cropData),
    sendCaptureFile: (base64Data: string) => ipcRenderer.send('capture-file', base64Data),
    onCaptureFile: (callback: (base64Data: string) => void) => {
      ipcRenderer.on('capture-file', (_, base64Data) => callback(`data:image/png;base64,${base64Data}`));
    },
    getAuthKeys: () => {
      return {
        aw_jwt: localStorage.getItem('aw_jwt'),
        uploadKey: localStorage.getItem('uploadKey'),
      };
    },
    sendToMain: (channel: string, data: any) => {
      ipcRenderer.send(channel, data);
    },
    onUploadFailed: (callback: (message: string) => void) => {
      ipcRenderer.on('upload-failed', (_, message) => callback(message));
    },
  },
};

contextBridge.exposeInMainWorld('electron', electronHandler);

export type ElectronHandler = typeof electronHandler;
