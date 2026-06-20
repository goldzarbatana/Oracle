using UnityEngine;

namespace TimeAura.Core
{
    [CreateAssetMenu(menuName = "TimeAura/App Config", fileName = "AppConfig")]
    public sealed class AppConfig : ScriptableObject, IService
    {
        [Header("Economy (Horas - Time Currency)")]
        [SerializeField] private float initialHoras = 300f;
        [SerializeField] private float transformationMinHoras = 60f;

        [Header("Status & Progression")]
        [SerializeField] private int initialStatus = 0;

        [Header("Aspect Colors (Luxury Palette)")]
        [SerializeField] private Color lumenColor = new Color(0.529f, 0.808f, 0.922f); // #87CEEB Sky Blue
        [SerializeField] private Color formaColor = new Color(0.831f, 0.686f, 0.216f); // #D4AF37 Gold
        [SerializeField] private Color actionColor = new Color(0.863f, 0.078f, 0.235f); // #DC143C Crimson
        [SerializeField] private Color essenceColor = new Color(0.596f, 0.984f, 0.596f); // #98FB98 Pale Green

        [Header("Security (Twilio)")]
        [SerializeField] private string twilioAccountSid = "";
        [SerializeField] private string twilioAuthToken = "";
        [SerializeField] private string twilioFromNumber = "";

        [Header("Networking")]
        [SerializeField] private string apiBaseUrl = "https://api.timeaura.com";
        public string ApiBaseUrl => apiBaseUrl;

        [Header("Monetization & Limits")]
        [Tooltip("Кількість безкоштовних контрактів за період")]
        [SerializeField] private int freeContractsLimit = 1;

        [Tooltip("Період оновлення безкоштовних контрактів (у днях)")]
        [SerializeField] private int freeContractsPeriodDays = 1;

        [Header("Security & Visage")]
        [SerializeField] private bool requireVisageForHarmony = false;

        public int FreeContractsLimit => freeContractsLimit;
        public int FreeContractsPeriodDays => freeContractsPeriodDays;

        public float InitialHoras => initialHoras;
        public float TransformationMinHoras => transformationMinHoras;
        public int InitialStatus => initialStatus;
        public bool RequireVisageForHarmony => requireVisageForHarmony;

        public Color GetAspectColor(AspectType aspect)
        {
            return aspect switch
            {
                AspectType.Lumen => lumenColor,
                AspectType.Forma => formaColor,
                AspectType.Action => actionColor,
                AspectType.Essence => essenceColor,
                _ => Color.white
            };
        }

        public string TwilioAccountSid => twilioAccountSid;
        public string TwilioAuthToken => twilioAuthToken;
        public string TwilioFromNumber => twilioFromNumber;

        [Header("Оракул (Gemini AI & Qwen)")]
        [Tooltip("Увімкніть для локальної симуляції (заглушки) відповідей Оракула. Корисно для швидкого тестування UI та економіки без витрати лімітів реального API.")]
        [SerializeField] private bool simulateOracle = false;
        
        [Tooltip("РЕКОМЕНДОВАНО: Використовувати безпечний бекенд Firebase Cloud Functions. Якщо увімкнено, клієнт не зберігає API-ключі. Якщо вимкнено — запити йтимуть безпосередньо (може бути небезпечно для продакшену).")]
        [SerializeField] private bool useCloudFunctions = true;
        
        [Tooltip("Посилання на вашу розгорнуту Cloud Function 'askOracle' (наприклад: https://...cloudfunctions.net/askOracle). Залиште порожнім, якщо використовуєте вбудований Firebase SDK для виклику функцій.")]
        [SerializeField] private string oracleCloudFunctionUrl = "https://askoracle-uc.a.run.app";

        [Header("Alibaba Qwen Config")]
        [Tooltip("API key for Alibaba Cloud DashScope (Qwen). Temporary client-side storage for Hackathon.")]
        [SerializeField] private string qwenApiKey = "sk-ws-H.IPDXEX.iBoa.MEUCIQDQZLtdYM5PikVHkMrVi9rREtZeXsKRtnoHXweoewhKrAIgXBbRe6KxSmtQWZeyYWTqUZDwaCT_LkjZ2i7lhCTKqI8";
        public string QwenApiKey => qwenApiKey;

        [Header("Google Sheets Remote Config")]
        [Tooltip("The URL to the published Google Sheet (without query parameters). Example: https://docs.google.com/spreadsheets/d/e/2PACX-1v.../pub")]
        [SerializeField] private string googleSheetBaseUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vSdUDeV1qOd9sH0-CdJ0jIvAbOqXl_qypNgmEhXszmP-YlwnJkYpJAK5ezVCJCRAFqKlZzqUGR1lbe9/pub";

        [Header("Stripe Payment Gateway")]
        [Tooltip("Використовувати реальну інтеграцію з Stripe у Test Mode замість Mock-версії. Увімкніть для тестування справжнього UI оплати.")]
        [SerializeField] private bool useRealStripeTestMode = true;

        public bool SimulateOracle => simulateOracle;
        public bool UseCloudFunctions => useCloudFunctions;
        public string OracleCloudFunctionUrl => oracleCloudFunctionUrl;
        public string GoogleSheetBaseUrl => googleSheetBaseUrl;
        public bool UseRealStripeTestMode => useRealStripeTestMode;
    }
}