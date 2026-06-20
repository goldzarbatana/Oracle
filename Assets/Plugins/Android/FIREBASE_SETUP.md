# Firebase Android Setup - Time Aura

## ✅ Status: Configured

Gradle templates налаштовані для роботи з Firebase.

## 📁 File Structure

```
Assets/Plugins/Android/
├── google-services.json          ← Firebase config (YOU ADDED THIS ✓)
├── baseProjectTemplate.gradle    ← Project-level (google-services plugin added ✓)
├── mainTemplate.gradle           ← App-level (apply plugin added ✓)
├── launcherTemplate.gradle       ← Launcher config
└── settingsTemplate.gradle       ← Settings config
```

## 🔧 What Was Configured

### 1. **baseProjectTemplate.gradle**
Added Google Services plugin to project-level:
```groovy
id 'com.google.gms.google-services' version '4.4.2' apply false
```

### 2. **mainTemplate.gradle**
Applied the plugin at app-level:
```groovy
apply plugin: 'com.google.gms.google-services'
```

### 3. **Firebase Dependencies**
Already managed by External Dependency Manager (EDM4U):
- ✅ `firebase-analytics`
- ✅ `firebase-auth`
- ✅ `firebase-firestore`
- ✅ `firebase-messaging`
- ✅ `firebase-appcheck`

## 🚀 Next Steps

### 1. Build Android APK
```
Unity → File → Build Settings → Android → Build
```

### 2. Check Build Logs
If errors occur during Gradle sync:
- Look for `google-services.json` parsing errors
- Verify package name matches Firebase Console
- Check Gradle version compatibility

### 3. Test Authentication
After successful build:
- Test Phone Auth (SMS verification)
- Verify Firestore read/write
- Check Analytics events in Firebase Console

## 🔍 Troubleshooting

### Error: "google-services.json not found"
- Ensure file is in `Assets/Plugins/Android/`
- Verify file name is exactly `google-services.json`
- Rebuild project

### Error: "Package name mismatch"
- Check Firebase Console → Project Settings → Android App
- Verify `applicationId` in Unity (Edit → Project Settings → Player → Android → Package Name)
- Must match exactly

### Error: "Plugin version conflict"
- Check Unity's AGP version (currently 8.10.0)
- Google Services 4.4.2 is compatible with AGP 8.x
- If issues persist, try 4.3.15

### Error: "Duplicate class found"
- EDM4U might have conflicts
- Go to Assets → External Dependency Manager → Android Resolver → Settings
- Try "Force Resolve"

## 📱 Package Name
Make sure your Unity package name matches Firebase config:
- **Unity:** Edit → Project Settings → Player → Android → Package Name
- **Firebase Console:** Project Settings → Your apps → Android app

Example:
```
Unity Package: com.timeaura.app
Firebase Package: com.timeaura.app  ← MUST MATCH!
```

## 🔒 SHA-1 Certificate (Required for Phone Auth)
To enable SMS verification:
1. Get SHA-1 from your keystore:
   ```bash
   keytool -list -v -keystore path/to/your.keystore -alias your_alias
   ```
2. Add to Firebase Console → Project Settings → Your Android app
3. Download updated `google-services.json`
4. Replace the file in `Assets/Plugins/Android/`

## ✨ Firebase Unity SDK Version
Current: **13.6.0** (via EDM4U)

Check for updates:
- https://firebase.google.com/docs/unity/setup

## 📚 References
- [Firebase Unity Setup](https://firebase.google.com/docs/unity/setup)
- [Android Gradle Plugin compatibility](https://developer.android.com/studio/releases/gradle-plugin)
- [Unity Gradle overview](https://docs.unity3d.com/Manual/android-gradle-overview.html)

---

**Status:** ✅ Ready for Android build
**Last Updated:** 2026-02-28
