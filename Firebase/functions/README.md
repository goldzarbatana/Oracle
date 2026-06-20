# Time Aura - Firebase Cloud Functions

## Overview
Server-side functions for secure Vector management and Transformation processing.

## Functions

### `updateVectors`
**Type:** Callable HTTPS  
**Purpose:** Safely updates user Vectors after a Transformation session completes.

**Parameters:**
```javascript
{
  sessionId: string,
  initiatorId: string,
  recipientId: string,
  vectorsExchanged: number,
  resonanceLevel: number (1-5)
}
```

**Returns:**
```javascript
{
  success: boolean,
  message: string
}
```

### `createTransformation`
**Type:** Callable HTTPS  
**Purpose:** Creates a new Transformation session.

**Parameters:**
```javascript
{
  recipientId: string,
  vectorsToExchange: number
}
```

**Returns:**
```javascript
{
  success: boolean,
  sessionId: string
}
```

### `updateResonance`
**Type:** Firestore Trigger  
**Purpose:** Automatically updates user Resonance stats when a Transformation completes.

**Triggers on:** `transformations/{sessionId}` document update

## Deployment

1. Install dependencies:
```bash
cd Firebase/functions
npm install
```

2. Test locally:
```bash
npm run serve
```

3. Deploy to Firebase:
```bash
npm run deploy
```

## Security Rules

These functions enforce:
- Authentication checks
- Vector balance validation
- Transaction atomicity
- Session integrity
- Anti-cheat measures

## Unity Integration

Call from Unity using Firebase SDK:

```csharp
var updateVectors = FirebaseFunctions.DefaultInstance.GetHttpsCallable("updateVectors");
var data = new Dictionary<string, object>
{
    { "sessionId", session.sessionId },
    { "initiatorId", session.initiatorUserId },
    { "recipientId", session.recipientUserId },
    { "vectorsExchanged", session.vectorsExchanged },
    { "resonanceLevel", (int)resonance }
};

var result = await updateVectors.CallAsync(data);
```

## Monitoring

View logs:
```bash
npm run logs
```

Or in Firebase Console: Functions → Logs
