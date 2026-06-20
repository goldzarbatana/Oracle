const functions = require('firebase-functions');
const stripe = require('stripe')(functions.config().stripe.secret_key);

// Cloud Function для створення Stripe Checkout Session
exports.createStripeCheckoutSession = functions.https.onCall(async (data, context) => {
  // Перевіряємо аутентифікацію користувача
  if (!context.auth) {
    throw new functions.https.HttpsError(
      'unauthenticated',
      'Функція доступна тільки авторизованим користувачам.'
    );
  }

  // Отримуємо параметри з клієнта
  const { sessionId, amountCents, currency, description, successUrl, cancelUrl } = data;

  // Валідація параметрів
  if (!sessionId || typeof sessionId !== 'string') {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідний ідентифікатор сесії (sessionId).'
    );
  }

  if (!amountCents || typeof amountCents !== 'number' || amountCents <= 0) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідну суму в центах (amountCents).'
    );
  }

  if (!currency || typeof currency !== 'string') {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідну валюту (currency).'
    );
  }

  if (!successUrl || typeof successUrl !== 'string') {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідний URL успіху (successUrl).'
    );
  }

  if (!cancelUrl || typeof cancelUrl !== 'string') {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідний URL скасування (cancelUrl).'
    );
  }

  try {
    // Створюємо Stripe Checkout Session
    const session = await stripe.checkout.sessions.create({
      payment_method_types: ['card'],
      line_items: [
        {
          price_data: {
            currency: currency.toLowerCase(),
            product_data: {
              name: description || 'TimeAura Escrow Payment',
              description: 'Оплата ескроу послуг TimeAura',
            },
            unit_amount: amountCents,
          },
          quantity: 1,
        },
      ],
      mode: 'payment',
      success_url: successUrl,
      cancel_url: cancelUrl,
      customer_email: context.auth.token.email || null,
      metadata: {
        userId: context.auth.uid,
        sessionId: sessionId,
        timestamp: Date.now().toString()
      }
    });

    // Повертаємо URL сесії
    return {
      url: session.url,
      sessionId: session.id
    };
  } catch (error) {
    console.error('Помилка при створенні Stripe Checkout Session:', error);
    throw new functions.https.HttpsError(
      'internal',
      'Не вдалося створити сесію оплати: ' + error.message
    );
  }
});

// Додаткові функції для управління ескроу
exports.createPaymentIntent = functions.https.onCall(async (data, context) => {
  // Перевіряємо аутентифікацію користувача
  if (!context.auth) {
    throw new functions.https.HttpsError(
      'unauthenticated',
      'Функція доступна тільки авторизованим користувачам.'
    );
  }

  const { amountCents, currency } = data;

  if (!amountCents || typeof amountCents !== 'number' || amountCents <= 0) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідну суму в центах (amountCents).'
    );
  }

  try {
    const paymentIntent = await stripe.paymentIntents.create({
      amount: amountCents,
      currency: currency.toLowerCase(),
      metadata: {
        userId: context.auth.uid
      }
    });

    return {
      clientSecret: paymentIntent.client_secret
    };
  } catch (error) {
    console.error('Помилка при створенні Payment Intent:', error);
    throw new functions.https.HttpsError(
      'internal',
      'Не вдалося створити платіж: ' + error.message
    );
  }
});

// Функція для випуску коштів з ескроу
exports.releaseEscrow = functions.https.onCall(async (data, context) => {
  // Перевіряємо аутентифікацію користувача
  if (!context.auth) {
    throw new functions.https.HttpsError(
      'unauthenticated',
      'Функція доступна тільки авторизованим користувачам.'
    );
  }

  const { sessionId } = data;

  if (!sessionId || typeof sessionId !== 'string') {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідний ідентифікатор сесії (sessionId).'
    );
  }

  try {
    // У реальному застосунку тут буде логіка випуску коштів з ескроу
    // Поки що просто повертаємо успішний результат для тестування
    
    console.log(`Емуляція випуску коштів з ескроу для сесії: ${sessionId}`);
    
    return {
      success: true,
      message: `Ескроу для сесії ${sessionId} успішно випущено`,
      timestamp: Date.now()
    };
  } catch (error) {
    console.error('Помилка при випуску коштів з ескроу:', error);
    throw new functions.https.HttpsError(
      'internal',
      'Не вдалося випустити кошти з ескроу: ' + error.message
    );
  }
});

// Функція для повернення коштів з ескроу
exports.refundEscrow = functions.https.onCall(async (data, context) => {
  // Перевіряємо аутентифікацію користувача
  if (!context.auth) {
    throw new functions.https.HttpsError(
      'unauthenticated',
      'Функція доступна тільки авторизованим користувачам.'
    );
  }

  const { sessionId } = data;

  if (!sessionId || typeof sessionId !== 'string') {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Потрібно надати валідний ідентифікатор сесії (sessionId).'
    );
  }

  try {
    // У реальному застосунку тут буде логіка повернення коштів з ескроу
    // Поки що просто повертаємо успішний результат для тестування
    
    console.log(`Емуляція повернення коштів з ескроу для сесії: ${sessionId}`);
    
    return {
      success: true,
      message: `Ескроу для сесії ${sessionId} успішно повернено`,
      timestamp: Date.now()
    };
  } catch (error) {
    console.error('Помилка при поверненні коштів з ескроу:', error);
    throw new functions.https.HttpsError(
      'internal',
      'Не вдалося повернути кошти з ескроу: ' + error.message
    );
  }
});