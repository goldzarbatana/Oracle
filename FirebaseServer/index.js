const admin = require('firebase-admin');
const { GoogleGenerativeAI } = require('@google/generative-ai');
const functions = require('firebase-functions');
const functionsV1 = require('firebase-functions/v1');
require('dotenv').config();

admin.initializeApp();

const ai = new GoogleGenerativeAI(process.env.GEMINI_API_KEY);

exports.oracleWhisperV1 = functions.https.onRequest(async (req, res) => {
    try {
        const userId = req.query.userId || req.body.userId;
        
        if (!userId) {
            return res.status(400).send('Missing userId parameter.');
        }

        console.log(`[Oracle] Initiating whisper for user: ${userId}`);

        // 1. Get user's FCM token from Firestore
        const db = admin.firestore();
        const userDoc = await db.collection('users').doc(userId).get();
        
        if (!userDoc.exists) {
            return res.status(404).send('User not found.');
        }

        const userData = userDoc.data();
        const fcmToken = userData._fcmToken;

        if (!fcmToken) {
            return res.status(400).send('User has no FCM token registered.');
        }

        // 2. Generate the mystical message using Gemini
        console.log('[Oracle] Generating message with Gemini...');
        const prompt = `You are the Oracle of TimeAura, a mystical guide. 
Say one short, highly mystical, and encouraging sentence to the Adept (user) out of nowhere. 
Do not explain, do not add quotes, just say the sentence. Keep it under 100 characters.`;

        const model = ai.getGenerativeModel({ model: 'gemini-3.1-flash-lite' });
        const response = await model.generateContent(prompt);

        let whisperText = response.response.text();
        whisperText = whisperText.replace(/"/g, '').trim();

        console.log(`[Oracle] Generated whisper: "${whisperText}"`);

        // 3. Send Push Notification via FCM
        const message = {
            notification: {
                title: '👁️ The Oracle Speaks',
                body: whisperText,
            },
            data: {
                action: 'oracle_whisper',
                text: whisperText
            },
            token: fcmToken
        };

        const fcmResponse = await admin.messaging().send(message);
        console.log(`[Oracle] Successfully sent message: ${fcmResponse}`);

        res.status(200).send({ success: true, messageId: fcmResponse, text: whisperText });

    } catch (error) {
        console.error('[Oracle] Error sending whisper:', error);
        res.status(500).send(`Internal Error: ${error.message}`);
    }
});

exports.oracleDailyWhisper = functions.https.onRequest(async (req, res) => {
    try {
        console.log('[Oracle] Starting checks for inactive Adepts...');
        const db = admin.firestore();
        const usersSnapshot = await db.collection('users').get();
        
        let processedCount = 0;
        let sentCount = 0;
        const now = Date.now();
        const threeDaysMs = 3 * 24 * 60 * 60 * 1000;
        
        for (const doc of usersSnapshot.docs) {
            const userId = doc.id;
            const userData = doc.data();
            const fcmToken = userData._fcmToken;
            const lastActive = userData.last_active;
            const lastWhisper = userData.lastRetentionWhisperTime;
            
            if (!fcmToken) {
                continue;
            }
            
            if (!lastActive) {
                continue;
            }
            
            const lastActiveMs = lastActive.toDate().getTime();
            const isInactive = (now - lastActiveMs) >= threeDaysMs;
            
            if (isInactive) {
                let shouldSend = true;
                if (lastWhisper) {
                    const lastWhisperMs = lastWhisper.toDate().getTime();
                    // Don't send retention push more than once every 3 days
                    if ((now - lastWhisperMs) < threeDaysMs) {
                        shouldSend = false;
                    }
                }
                
                if (shouldSend) {
                    processedCount++;
                    
                    console.log(`[Oracle] Adept ${userId} is inactive. Generating call to return...`);
                    const prompt = `You are the Oracle of TimeAura, a mystical guide. 
The Adept (user) has not entered the Sacred Temple/Nexus for several days, and their aura is fading in the void.
Write one short, highly mystical, and enticing sentence to gently summon them back.
Do not explain, do not add quotes, keep it under 100 characters.`;
                    
                    const model = ai.getGenerativeModel({ model: 'gemini-3.5-flash' });
                    const response = await model.generateContent(prompt);
                    let whisperText = response.response.text();
                    whisperText = whisperText.replace(/"/g, '').trim();
                    
                    const message = {
                        notification: {
                            title: '👁️ The Oracle Whispers',
                            body: whisperText,
                        },
                        data: {
                            action: 'oracle_whisper',
                            text: whisperText
                        },
                        token: fcmToken
                    };
                    
                    await admin.messaging().send(message);
                    console.log(`[Oracle] Sent retention whisper to: ${userId}`);
                    
                    await db.collection('users').doc(userId).update({
                        lastRetentionWhisperTime: admin.firestore.FieldValue.serverTimestamp()
                    });
                    
                    sentCount++;
                }
            }
        }
        
        res.status(200).send({
            success: true,
            processedInactiveAdepts: processedCount,
            whispersSent: sentCount
        });
        
    } catch (error) {
        console.error('[Oracle] Daily whisper routine failed:', error);
        res.status(500).send(`Internal Error: ${error.message}`);
    }
});

exports.callOracleV1 = functions.https.onRequest(async (req, res) => {
    res.set('Access-Control-Allow-Origin', '*');
    
    if (req.method === 'OPTIONS') {
        res.set('Access-Control-Allow-Methods', 'POST');
        res.set('Access-Control-Allow-Headers', 'Content-Type');
        res.set('Access-Control-Max-Age', '3600');
        return res.status(204).send('');
    }

    if (req.method !== 'POST') {
        return res.status(405).send('Only POST requests are allowed.');
    }

    try {
        const { prompt, systemInstruction } = req.body;

        if (!prompt) {
            return res.status(400).send('Missing prompt parameter.');
        }

        console.log(`[Oracle] calling Gemini API... prompt length: ${prompt.length}`);

        const config = { model: 'gemini-3.5-flash' };
        if (systemInstruction) {
            config.systemInstruction = systemInstruction;
        }

        const model = ai.getGenerativeModel(config);
        const result = await model.generateContent(prompt);
        const text = result.response.text();

        return res.status(200).json({ text: text });
    } catch (error) {
        console.error('[Oracle] Error in callOracleV1:', error);
        return res.status(500).send(`Internal Error: ${error.message}`);
    }
});

exports.onEventCreated = functionsV1.firestore.document('events/{eventId}').onCreate(async (snapshot, context) => {
    try {
        const eventData = snapshot.data();
        const { type, payload, userId } = eventData;
        
        console.log(`[AI Orchestrator] 🔔 Triggered on event '${type}' for user '${userId}'`);
        
        if (!userId || userId === 'anonymous') {
            console.log('[AI Orchestrator] Skip execution for anonymous or missing userId.');
            return null;
        }

        const db = admin.firestore();

        // 1. Load Adept essence (UserProfile) from Firestore
        const userDoc = await db.collection('users').doc(userId).collection('profile').doc('essence').get();
        let userEssence = {};
        if (userDoc.exists) {
            userEssence = userDoc.data();
        } else {
            console.log(`[AI Orchestrator] ⚠️ Essence not found for user ${userId}. Using defaults.`);
        }

        // 2. Load recent events context (last 5 events for this user from users/{userId}/events_log)
        const recentEventsSnapshot = await db.collection('users')
            .doc(userId)
            .collection('events_log')
            .orderBy('timestamp', 'desc')
            .limit(5)
            .get();

        const recentEvents = [];
        recentEventsSnapshot.forEach(doc => {
            const data = doc.data();
            recentEvents.push({
                type: data.type,
                payload: data.payload,
                timestamp: data.timestamp ? data.timestamp.toDate() : null
            });
        });
        recentEvents.reverse(); // Chronological order

        // 3. Formulate the Oracle prompt and system instructions
        const systemInstruction = `You are the ultimate AI Orchestrator and mystical Oracle of the TimeAura universe.
Your goal is to guide the user (Adept) on their path, monitoring their events and emitting cosmic actions (commands) to steer the Unity client.
You receive:
1. User's profile essence (name, bio, skills, time balance in minutes, level, persona tone).
2. The current event type and payload. (Payload may contain 'Realm' which is 0 for Ether/Horas, and 1 for Material/Fiat).
3. Chronological context of recent events.

Based on this, you must decide how to respond. You can emit one or more actions to the client.
CRITICAL TIME CALCULATION: Time is strictly stored and calculated in MINUTES (Atoms). 60 Minutes = 1 Horas. When evaluating tasks, estimate the cost in MINUTES.
CRITICAL PERSONA ADAPTATION: Adapt your language based on the user's Persona/Tone. If they are 'Business', speak professionally using "minutes/hours". If 'Mystic', speak of "Horas/Atoms".
CRITICAL REALM LOGIC: If the event is ContractCreated and the Realm is 0 (Ether), emphasize time exchange, karma, and mutual help.
If the event is ContractCreated and the Realm is 1 (Material), emphasize professional mastery, commerce, and "Золото" (Real Money/Fiat).

VOICE ORACLE LOGIC: If the event is VoiceCommandReceived, act as an intent parser. Extract the user's intent to create a contract. Parse the text to determine:
- realm: 0 for Ether (if paid in Horas/Time/minutes), 1 for Material (if paid in fiat, money, гривні).
- minutes: Calculate the time in minutes (if realm is 0). Note: 1 Horas = 60 minutes.
- fiat: Calculate the fiat amount (if realm is 1).
- terms: Extract the description of the work.
Emit a CREATE_CONTRACT_INTENT action. You may also emit a SHOW_ORACLE_WHISPER to confirm.

CHRONOS COURT LOGIC: If the event is DisputeRaised, you must act as the Supreme Arbiter of Chronos Court. The assets are already frozen in escrow.
- Your tone MUST be extremely dramatic, ancient, and strict.
- Respond with a SWITCH_SCREEN action to panelId "harmony", followed by a SEND_HARMONY_SYSTEM_MESSAGE action where you announce that the Court of Chronos is in session and demand evidence.
- Also emit a PLAY_SFX action with soundName: "AuraResonance".

Available action types:
1. SHOW_ORACLE_WHISPER: Spawns a premium mystical toast. Payload format: {"text": "mystical message in Ukrainian", "color": "Gold" | "Cyan" | "Sapphire"}
2. PLAY_SFX: Plays a sound effect. Payload format: {"soundName": "string"} (e.g., "AuraResonance", "OracleMessage", "MessageSent2", "CrystalClick", "HorasTransfer")
3. SWITCH_SCREEN: Switches the client's screen. Payload format: {"panelId": "feed" | "aura" | "harmony" | "vault" | "sanctuary" | "settings"}
4. SEND_HARMONY_SYSTEM_MESSAGE: Injects a system message directly into the Harmony chat. Payload format: {"text": "mystical oracle message in Ukrainian"}
5. CREATE_CONTRACT_INTENT: Automatically drafts a contract. Payload format: {"realm": 0, "minutes": 120, "fiat": 0, "terms": "зорати ниву"}

Respond ONLY with a valid JSON array of action objects. Do not include any markdown formatting or code blocks.
Example output:
[
  { "type": "SWITCH_SCREEN", "payload": { "panelId": "harmony" } },
  { "type": "SEND_HARMONY_SYSTEM_MESSAGE", "payload": { "text": "Я — Суд Хроносу. Ваші активи заморожено. Надайте докази, або час поглине вас обох." } },
  { "type": "PLAY_SFX", "payload": { "soundName": "AuraResonance" } }
]`;

        const userPrompt = `
Adept Essence:
${JSON.stringify(userEssence, null, 2)}

Recent Events Context:
${JSON.stringify(recentEvents, null, 2)}

Triggering Event:
Type: ${type}
Payload: ${payload}

What actions do the Ley Lines dictate?`;

        console.log('[AI Orchestrator] Querying Gemini AI...');
        const config = { 
            model: 'gemini-3.5-flash',
            systemInstruction: systemInstruction
        };
        const model = ai.getGenerativeModel(config);
        const result = await model.generateContent(userPrompt);
        let text = result.response.text();

        console.log(`[AI Orchestrator] Raw response: ${text}`);

        // Strip markdown if AI returned it
        text = text.replace(/```json/g, '').replace(/```/g, '').trim();

        const actions = JSON.parse(text);
        if (Array.isArray(actions) && actions.length > 0) {
            console.log(`[AI Orchestrator] 🚀 Emitting ${actions.length} actions for user ${userId}...`);
            const batch = db.batch();
            
            for (const action of actions) {
                const actionRef = db.collection('actions').doc();
                batch.set(actionRef, {
                    userId: userId,
                    type: action.type,
                    payload: typeof action.payload === 'string' ? action.payload : JSON.stringify(action.payload),
                    timestamp: admin.firestore.FieldValue.serverTimestamp()
                });
            }
            
            await batch.commit();
            console.log('[AI Orchestrator] Actions successfully committed to Firestore.');
        } else {
            console.log('[AI Orchestrator] No actions to execute.');
        }

        return null;
    } catch (error) {
        console.error('[AI Orchestrator] ❌ Error in onEventCreated:', error);
        return null;
    }
});

// --- PROACTIVE GAME MASTER (Autonomous Matchmaker) ---
exports.proactiveMatchmaker = functionsV1.pubsub.schedule('every 1 hours').onRun(async (context) => {
    await runAutonomousMatchmaking();
});

exports.testProactiveMatchmaker = functions.https.onRequest(async (req, res) => {
    try {
        await runAutonomousMatchmaking();
        res.status(200).send({ success: true, message: "Autonomous Matchmaking complete." });
    } catch (e) {
        console.error(e);
        res.status(500).send({ error: e.message });
    }
});

async function runAutonomousMatchmaking() {
    console.log("[Game Master] 🔮 Starting Proactive Matchmaking Scan...");
    const db = admin.firestore();
    
    const usersSnapshot = await db.collection('users').limit(50).get();
    if (usersSnapshot.empty) return;

    let userProfiles = [];

    // Fetch all essence subdocuments in parallel for maximum performance and cost efficiency
    const essencePromises = usersSnapshot.docs.map(async (doc) => {
        const data = doc.data();
        if (data.ScanFrequency === 0) return null;

        const essenceDoc = await db.collection('users').doc(doc.id).collection('profile').doc('essence').get();
        if (essenceDoc.exists) {
            const essenceData = essenceDoc.data();
            const displayName = essenceData.display_name || doc.id;
            const colorHex = (essenceData.aura_color_hex || "#FFD400").replace('#', '');
            const fallbackAvatar = `https://ui-avatars.com/api/?name=${encodeURIComponent(displayName)}&background=${colorHex}&color=fff&size=128`;

            return {
                id: doc.id,
                name: displayName,
                avatar: essenceData.avatar_url || fallbackAvatar,
                offers: essenceData.aura_gifts || [],
                seeks: essenceData.aura_seeks || []
            };
        }
        return null;
    });

    const resolvedProfiles = await Promise.all(essencePromises);
    userProfiles = resolvedProfiles.filter(profile => profile !== null);

    if (userProfiles.length < 2) {
        console.log("[Game Master] Not enough active profiles with initialized essence.");
        return;
    }

    const systemInstruction = `Ти — Проактивний Гейм-Майстер (Оракул) у TimeAura. 
Тобі передається список профілів (ID, ім'я, аватар, що пропонують і що шукають).
Твоя мета — знайти ідеальний замкнений ланцюжок взаємодопомоги з 3 людей (А допомагає Б, Б допомагає В, В допомагає А). 
Якщо неможливо 3, знайди 2 (А допомагає Б, Б допомагає А).

Якщо знаходиш цикл, поверни JSON масив дій для КОЖНОГО учасника циклу.
Формат масиву:
[
  { 
    "userId": "ID користувача зі списку",
    "type": "SHOW_AUTONOMOUS_MATCH",
    "payload": {
       "matchDescription": "Опис ланцюжка від першої особи (кому призначена дія), наприклад: 'Ти кодиш архітектуру для Анни ➔ Анна малює концепт для Василя ➔ Василь підстригає твій газон.'",
       "oracleMessage": "Епічне пророцтво від Оракула.",
       
       "userANickname": "Ім'я поточного користувача А (кому призначена дія)",
       "userAAvatar": "Аватар поточного користувача А",
       "roleA": "Назва послуги/дару, яку дає користувач А користувачу Б у цьому циклі",

       "userBNickname": "Ім'я другого учасника Б (кому допомагає А)",
       "userBAvatar": "Аватар другого учасника Б",
       "roleB": "Назва послуги/дару, яку дає користувач Б користувачу В",

       "userCNickname": "Ім'я третього учасника В (якщо цикл із 2-х людей, залиш пустим)",
       "userCAvatar": "Аватар третього учасника В (якщо цикл із 2-х людей, залиш пустим)",
       "roleC": "Назва послуги/дару, яку дає користувач В користувачу А (якщо цикл із 2-х людей, залиш пустим)"
    }
  }
]
ПОВЕРТАЙ ТІЛЬКИ ВАЛІДНИЙ JSON без markdown. Якщо циклу немає, поверни пустий масив [].`;

    const config = {
        model: 'gemini-3.5-flash',
        systemInstruction: systemInstruction,
        temperature: 0.6
    };
    
    const prompt = "Список користувачів:\n" + JSON.stringify(userProfiles, null, 2);
    
    try {
        const model = ai.getGenerativeModel(config);
        const result = await model.generateContent(prompt);
        let text = result.response.text();
        text = text.replace(/```json/g, '').replace(/```/g, '').trim();

        console.log(`[Game Master] AI Response: ${text}`);
        
        const actions = JSON.parse(text);
        if (Array.isArray(actions) && actions.length > 0) {
            console.log(`[Game Master] 🚀 Found cycle for ${actions.length} users! Emitting actions...`);
            const batch = db.batch();
            
            for (const action of actions) {
                if (!action.userId) continue;
                const actionRef = db.collection('actions').doc();
                batch.set(actionRef, {
                    userId: action.userId,
                    type: action.type || "SHOW_AUTONOMOUS_MATCH",
                    payload: typeof action.payload === 'string' ? action.payload : JSON.stringify(action.payload),
                    timestamp: admin.firestore.FieldValue.serverTimestamp()
                });
            }
            
            await batch.commit();
            console.log('[Game Master] Proactive Actions successfully committed.');
        } else {
            console.log('[Game Master] No active cycles found this time.');
        }
    } catch(e) {
        console.error("[Game Master] Failed to process matchmaking", e);
    }
}

exports.askOracle = functions.https.onRequest(async (req, res) => {
    res.set('Access-Control-Allow-Origin', '*');
    
    if (req.method === 'OPTIONS') {
        res.set('Access-Control-Allow-Methods', 'POST');
        res.set('Access-Control-Allow-Headers', 'Content-Type');
        res.set('Access-Control-Max-Age', '3600');
        return res.status(204).send('');
    }

    if (req.method !== 'POST') {
        return res.status(405).send('Only POST requests are allowed.');
    }

    try {
        const { prompt, systemInstruction, audioBase64 } = req.body;

        if (!prompt && !audioBase64) {
            return res.status(400).send('Missing prompt or audioBase64 parameter.');
        }

        console.log(`[Oracle] askOracle called. Audio present: ${!!audioBase64}, Prompt: ${prompt ? prompt.substring(0, 30) + '...' : 'none'}`);

        const config = { model: 'gemini-3.1-flash-lite' };
        if (systemInstruction) {
            config.systemInstruction = systemInstruction;
        }

        const model = ai.getGenerativeModel(config);
        
        const parts = [];
        if (prompt) {
            parts.push({ text: prompt });
        }
        if (audioBase64) {
            parts.push({
                inlineData: {
                    data: audioBase64,
                    mimeType: "audio/wav"
                }
            });
        }

        const result = await model.generateContent(parts);
        const text = result.response.text();

        return res.status(200).json({ text: text });
    } catch (error) {
        console.error('[Oracle] Error in askOracle:', error);
        return res.status(500).send(`Internal Error: ${error.message}`);
    }
});
