# TimeAura Social Network - Modern Architecture Setup

## 📦 Required Unity Packages (2026 Stack)

### Core Performance (ОБОВ'ЯЗКОВО)
```bash
# Unity Package Manager або через manifest.json
```

**Already Installed:**
- ✅ UniTask (Cysharp.Threading.Tasks)
- ✅ Addressables (com.unity.addressables)
- ✅ TextMeshPro
- ✅ Zenject/Extenject (DI)

**Add These:**
```json
{
  "dependencies": {
    "com.unity.addressables": "2.0.8",
    "com.unity.netcode.gameobjects": "1.12.0",
    "com.cysharp.unitask": "2.5.4",
    "com.neuecc.unirx": "7.1.0",
    "com.unity.textmeshpro": "3.2.0-pre.7",
    "com.unity.ui": "2.0.0-pre.3",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

### Додаткові пакети (з GitHub/Asset Store)

1. **MessagePack for C#** (швидка серіалізація)
   ```
   https://github.com/neuecc/MessagePack-CSharp
   Add via UPM Git URL: https://github.com/neuecc/MessagePack-CSharp.git?path=src/MessagePack.UnityClient/Assets/Scripts/MessagePack
   ```

2. **UniRx** (Reactive Extensions)
   ```
   https://github.com/neuecc/UniRx
   Add via UPM: https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts
   ```

3. **Best HTTP** (покращений WebSocket/HTTP)
   ```
   Asset Store: https://assetstore.unity.com/packages/tools/network/best-http-267636
   Альтернатива: Native WebSocket
   ```

4. **VContainer** (швидший DI, альтернатива Zenject)
   ```
   https://github.com/hadashiA/VContainer
   com.vcontainer: https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#2.16.2
   ```

---

## 🏗️ Project Structure (Recommended)

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Services/
│   │   │   ├── NetworkService.cs ✅ CREATED
│   │   │   ├── AddressableAssetService.cs ✅ CREATED
│   │   │   └── CacheService.cs (for offline data)
│   │   ├── Managers/
│   │   │   └── IManager.cs
│   │   └── Config/
│   │       └── AppConfig.cs
│   │
│   ├── Features/
│   │   ├── Social/
│   │   │   ├── SocialManager.cs ✅ CREATED
│   │   │   ├── UserProfile.cs ✅ CREATED
│   │   │   ├── FeedController.cs (UI logic)
│   │   │   └── PostView.cs (UI component)
│   │   │
│   │   ├── Authentication/
│   │   │   ├── AuthManager.cs
│   │   │   └── LoginView.cs
│   │   │
│   │   ├── Chat/
│   │   │   ├── ChatManager.cs (WebSocket)
│   │   │   └── MessageView.cs
│   │   │
│   │   └── Localization/
│   │       └── LocalizationManager.cs (update to UniTask)
│   │
│   └── UI/
│       ├── Common/ (reusable components)
│       ├── Screens/
│       └── Animations/
│
├── AddressableAssets/ (organize into groups)
│   ├── UI/
│   │   ├── Icons/
│   │   ├── Sprites/
│   │   └── Prefabs/
│   ├── Avatars/ (default avatars)
│   ├── Localization/ (string tables)
│   └── Media/ (templates, effects)
│
├── Plugins/
│   ├── UniTask/ ✅
│   ├── MessagePack/
│   └── UniRx/
│
└── StreamingAssets/ (fallback data)
```

---

## ⚙️ Addressables Setup for Social Network — "The Mystical Archives"

### 1. Create Addressable Groups (با Luxury Mysticism 🌙)

**Window → Asset Management → Addressables → Groups**

**Sacred Groups:**
- **`Relics`** — Interface artifacts (UI icons, buttons, sacred symbols)
  - *Former: UI_Common*
  - Purpose: Frequently used UI elements that form the temple's foundation
  
- **`Visages`** — The faces of Adepts (default avatar collection)
  - *Former: Avatars_Default*
  - Purpose: Default avatars for new initiates
  
- **`Chronicles`** — Stories and events (post templates, social interactions)
  - *Former: Social_Assets*
  - Purpose: Post templates, cards, feed elements
  
- **`Aura_Shards`** — Mystical effects & user-generated content (remote)
  - *Former: Remote_Content*
  - Purpose: Shader effects, particles, UGC from server
  
- **`Localization`** — The ancient tongues (string tables)
  - Purpose: Multilingual wisdom preserved across realms

### 2. Labels for Dynamic Loading (The Sacred Tags)

Create mystical labels:
- `relic-icon` (UI icons)
- `visage-default` (default avatars)
- `chronicle-template` (post templates)
- `aura-golden`, `aura-mystical` (shader effects)
- `tongue-en`, `tongue-uk`, `tongue-es` (localization)

### 3. Remote Catalog Setup (for Live Updates)

```csharp
// In AddressableAssetService.InitializeAsync
Addressables.AddResourceLocator(
    await Addressables.LoadContentCatalogAsync(
        "https://cdn.timeaura.com/catalogs/content.json"
    ).Task
);
```

### 4. Build Settings

**Edit → Project Settings → Addressables**
- Build Path: `ServerData/[BuildTarget]`
- Load Path: `https://cdn.timeaura.com/[BuildTarget]`
- Enable "Build Remote Catalog"
- Enable "Content Update Restriction"

---

## 🚀 Backend Recommendations

### Option A: Unity Gaming Services (найпростіший старт)

1. Enable in **Project Settings → Services**
2. Install packages:
   ```
   com.unity.services.authentication
   com.unity.services.cloudsave
   com.unity.services.lobby
   ```
3. Use with NetworkService:
   ```csharp
   await UnityServices.InitializeAsync();
   await AuthenticationService.Instance.SignInAnonymouslyAsync();
   ```

### Option B: Custom Backend (рекомендовано для масштабування)

**Tech Stack:**
- ASP.NET Core 8.0 + SignalR (WebSocket)
- PostgreSQL + Redis (cache)
- MinIO/S3 (media storage)
- Docker + Kubernetes
- Cloudflare CDN

**API Endpoints Structure:**
```
POST   /api/auth/login
POST   /api/auth/register
GET    /api/users/{id}
GET    /api/feed
POST   /api/posts
POST   /api/posts/{id}/like
GET    /api/posts/{id}/comments
POST   /api/uploads/image

WebSocket: wss://api.timeaura.com/hub/chat
```

### Option C: Firebase/Supabase (швидкий прототип)

**Firebase:**
- Authentication
- Firestore (NoSQL)
- Storage (images/video)
- Cloud Functions

**Supabase:** (open-source альтернатива)
- PostgreSQL
- Realtime subscriptions
- Storage
- Edge Functions

---

## 🎨 UI Modernization

### Migrate to UI Toolkit (UXML/USS)

**Why?** Швидше, сучасніше, схоже на веб-розробку

```csharp
// Example: FeedView.uxml
<ui:UXML>
    <ui:ScrollView name="feed-scroll">
        <ui:VisualElement name="post-container" />
    </ui:ScrollView>
</ui:UXML>

// FeedView.uss (CSS-like)
.post-card {
    background-color: #FFFFFF;
    border-radius: 12px;
    margin: 8px;
    padding: 16px;
}
```

### Alternative: Optimize UGUI with Nova

If staying with UGUI:
- Use **Nova UI** for performance (scrolling feeds)
- Object pooling for feed items
- Addressables для всіх sprites

---

## 📊 Performance Best Practices

### 1. Feed Pagination & Preloading
```csharp
// In SocialManager
public async UniTask<FeedResponse> GetFeedAsync(int page, int pageSize = 20)
{
    var feed = await _networkService.GetAsync<FeedResponse>($"feed?page={page}");
    
    // Preload next page assets
    if (feed.hasMore)
    {
        _ = PreloadFeedPage(page + 1);
    }
    
    return feed;
}
```

### 2. Image Caching Strategy
- **Memory cache:** Texture2D в Dictionary (current session)
- **Disk cache:** Application.persistentDataPath + hash
- **CDN cache:** Addressables remote catalogs

### 3. Async Everything with UniTask
```csharp
// ❌ BAD
IEnumerator LoadUser() {
    yield return StartCoroutine(...)
}

// ✅ GOOD
async UniTask LoadUserAsync(CancellationToken ct) {
    await _service.GetAsync<User>("users/me", ct);
}
```

### 4. Use Object Pooling for Feed Items
```csharp
// With Addressables + pooling
var pooledPost = await _assetService.InstantiateAsync(
    "post-card-prefab", 
    parent: feedContainer
);
// Reuse instead of Destroy
```

---

## 🔐 Security Recommendations

1. **Never store tokens in PlayerPrefs** (use Keychain/Keystore)
   ```csharp
   // Use Unity's SecurePlayerPrefs or platform-specific secure storage
   ```

2. **Validate all inputs** before sending to backend

3. **Use HTTPS/WSS only** for production

4. **Implement rate limiting** in NetworkService

5. **Sanitize UGC** (user-generated content) server-side

---

## 📈 Monitoring & Analytics

### Recommended Tools:
- **Unity Analytics** (basic metrics)
- **Mixpanel** (detailed user behavior)
- **Sentry** (crash reporting)
- **Firebase Performance Monitoring**

### Track These Metrics:
- Asset load times (Addressables)
- API response times
- Feed scroll performance (FPS)
- DAU/MAU, retention
- Network errors

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Test]
public async Task NetworkService_GetAsync_ReturnsData()
{
    var service = new NetworkService();
    var result = await service.GetAsync<UserProfile>("users/123");
    Assert.IsNotNull(result);
}
```

### Integration Tests
- Test full flow: login → load feed → create post
- Mock NetworkService for offline testing

### Performance Tests
- Benchmark feed loading with 1000+ items
- Memory profiling for image caching
- Stress test concurrent asset loads

---

## 🚦 Migration Checklist

- [ ] Install required packages
- [ ] Setup Addressables groups & labels
- [x] Create NetworkService with UniTask
- [x] Create AddressableAssetService
- [x] Create SocialManager
- [ ] Convert LocalizationManager to UniTask
- [ ] Create FeedView UI
- [ ] Implement WebSocket for chat/notifications
- [ ] Setup backend API or Firebase
- [ ] Configure CDN for Addressables
- [ ] Add caching layer
- [ ] Performance testing
- [ ] Deploy to TestFlight/Google Play Beta

---

## 📚 Additional Resources

**Documentation:**
- UniTask: https://github.com/Cysharp/UniTask
- Addressables: https://docs.unity3d.com/Packages/com.unity.addressables@latest
- UI Toolkit: https://docs.unity3d.com/Manual/UIElements.html
- MessagePack: https://github.com/neuecc/MessagePack-CSharp

**Example Projects:**
- UniTask Examples: https://github.com/Cysharp/UniTask/tree/master/src/UniTask/Assets/Scenes
- Addressables Sample: https://github.com/Unity-Technologies/Addressables-Sample

**Communities:**
- Unity Forums: https://forum.unity.com
- r/Unity3D: https://reddit.com/r/Unity3D
- Unity Discord

---

## ⚡ Quick Start Commands

```bash
# 1. Clone/Update packages
git submodule update --init --recursive

# 2. Build Addressables
# Window → Asset Management → Addressables → Build → New Build → Default Build Script

# 3. Run in Unity Editor
# Press Play - Addressables auto-loads from local build

# 4. Build for device
# File → Build Settings → Build
```

---

**Next Steps:**
1. ✅ Review created files: NetworkService, AddressableAssetService, SocialManager
2. Install missing packages (MessagePack, UniRx)
3. Convert LocalizationManager to UniTask
4. Create UI screens with UI Toolkit
5. Setup backend or Firebase
6. Test & iterate!

**Questions? Check:** [Unity Forums](https://forum.unity.com) | [GitHub Discussions](https://github.com/Cysharp/UniTask/discussions)
