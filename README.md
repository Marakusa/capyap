<img width="4096" height="1289" alt="banner" src="https://github.com/user-attachments/assets/bef4a8e1-365f-475b-909b-7a8b24a8239f" />

# CapYap
CapYap is a lightweight screen capturing app that lets you upload and manage your screenshots. This is pretty much just a practicing project and nothing serious. But if you want to try it out and enjoy using it, you are free to do so. This is just a standalone frontend for the app with the actual screen capturing function on desktop. The app for now only works on Windows, it haven't been tested on any other platforms yet.

The app uses Discord to authorize users. This makes it very quick and easy for the users to start using the app.

Backend runs on my server (https://capyap.marakusa.me)

### Backend repo
[capyap-backend](https://github.com/Marakusa/capyap-backend)

### Web interface repo
[capyap-web-frontend](https://github.com/Marakusa/capyap-web-frontend)

## Keybinds
```
Ctrl + Print Screen        : Take a screenshot
Esc                        : Close the cropping overlay
Shift (Hold)               : Crop to a window
Ctrl (Hold)                : Crop to a monitor
Alt (Hold)                 : Show magnifier
```

## TODO
- [x] Fix app blocking ESC key
- [x] Save and delete functions on image preview
- [x] Tray icon + background process
- [x] Optimize crop overlay
- [x] Fix monitor cropping when secondary monitor is on the left
- [ ] Add implementation for drawing over caps
- [ ] Update external changes (in dashboard and gallery)
- [ ] Mac + Linux support
