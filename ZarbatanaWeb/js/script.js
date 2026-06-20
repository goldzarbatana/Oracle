// Translations Dictionary
const translations = {
    en: {
        nav_about: "About",
        nav_company: "Company",
        nav_tech: "Tech Stack",
        nav_roadmap: "Roadmap",
        nav_investors: "Investors",
        nav_contact: "Contact",
        
        // Hero
        hero_subtitle: "Bridging the Digital Divide to Reconnect Real Communities",
        hero_desc: "Step away from the screen. We use AI not to keep you online, but to help you find and exchange skills with people right next door. Because true connection happens offline.",
        btn_tech_doc: "Technical Architecture",
        btn_contact: "Contact Us",
        
        // Manifesto
        manifesto_title: "OUR MANIFESTO",
        manifesto_1: "A neighbor is not just someone behind the wall. It’s someone nearby when you need them.",
        manifesto_2: "Time is more valuable than money. You can't earn it back.",
        manifesto_3: "The best algorithm is human empathy. AI only helps us find it.",
        manifesto_4: "True magic isn't in a screen. It's in the eyes of someone you helped.",
        manifesto_conclusion: "TimeAura is not an app. It's a movement to return to ourselves. To people. To life.",
        
        // Problem
        problem_title: "THE AGE OF LONELINESS",
        problem_subtitle: "We live in a time of paradox:",
        problem_1: "87% of city dwellers don't know their neighbors' names.",
        problem_2: "1 in 3 people experiences chronic loneliness.",
        problem_3: "We have 500 friends on Facebook, but no one to help move a couch.",
        problem_conclusion: "The internet connected continents but disconnected floors. We lost what our grandparents had naturally — a community where everyone knew everyone.",
        
        // About
        about_title: "What is TimeAura?",
        about_desc: "TimeAura is a community-first platform designed to restore the lost art of neighborhood mutual aid—bringing back the human connection that existed before the internet. A professional cook and a talented hairdresser might live on the same floor, unaware that one wants to learn how to bake a perfect pizza, while the other needs a stylish haircut. TimeAura bridges that gap. Our advanced AI completely orchestrates the platform—anticipating needs, securely managing matching, tracking skill exchanges, and automatically connecting neighbors who perfectly complement each other. We turn time into a currency of kindness, allowing neighbors within the same district or city to exchange services, help each other, and build real physical communities.",
        
        // Story
        story_title: "IMAGINE THIS...",
        story_p1: "Maria, 34, Kyiv. She's lived on the 5th floor for 3 years. She's a hairdresser. She cuts hair beautifully, but dreams of learning to bake Italian pizza.",
        story_p2: "On the 6th floor lives Andriy. A chef. He makes the best carbonara in town. But he needs a haircut — and he's too shy to go to a salon.",
        story_p3: "They live 3 meters apart. But they've never spoken.",
        story_p4: "TimeAura connected them in 4 seconds. The AI Oracle saw: 'Hairdresser nearby wants pizza. Chef nearby wants a haircut.'",
        story_p5: "Today, Maria is teaching Andriy how to make dough. Andriy is cutting Maria's hair on Saturday morning. They drink coffee on the balcony. They are friends.",
        story_conclusion: "It's not magic. It's the neighborhood we forgot.",

        // How users earn
        earn_title: "HOW TO EARN ON TIMEAURA?",
        earn_subtitle: "The platform opens multiple revenue streams for active community members.",
        earn_freelance_title: "FOR FREELANCERS",
        earn_f1_t: "Direct Orders",
        earn_f1_d: "Receive fiat payments for professional services (from $10 to $13,000+).",
        earn_f2_t: "Skill Barter",
        earn_f2_d: "Trade your services for others. Save money, get what you need.",
        earn_f3_t: "Reputation Building",
        earn_f3_d: "Every successful deal boosts your rating. More orders = more income.",
        earn_neighbor_title: "FOR NEIGHBORS",
        earn_n1_t: "Local Economy",
        earn_n1_d: "Earn Horas by helping neighbors. Spend them on local services.",
        earn_n2_t: "Micro-services",
        earn_n2_d: "Even 30 minutes of help has value. Move furniture, walk a dog, help with a PC.",
        earn_n3_t: "Community",
        earn_n3_d: "Build trust in your district. Become the 'reliable neighbor'.",
        earn_pro_title: "FOR PROFESSIONALS",
        earn_p1_t: "Enlightened Subscription",
        earn_p1_d: "Search priority, advanced analytics, VIP support.",
        earn_p2_t: "B2B Contracts",
        earn_p2_d: "Work directly with companies. TimeAura provides escrow and security.",
        earn_p3_t: "Scaling",
        earn_p3_d: "From individual gigs to retaining long-term clients.",
        earn_story_title: "Example: Olena, Web Designer",
        earn_story_1: "Month 1: 3 orders via TimeAura = $450",
        earn_story_2: "Month 3: 8 orders + 2 regular clients = $1,200",
        earn_story_3: "Month 6: Own studio, 3 assistants = $3,500/month",
        earn_conclusion: "TimeAura is not just an app. It's your path to financial independence.",
        
        // Features
        feature_1_title: "Real-World Impact",
        feature_1_desc: "Match with people in your neighborhood for physical, face-to-face service exchanges.",
        feature_2_title: "Time as Currency",
        feature_2_desc: "Trade your skills using Horas. 1 hour of cooking lesson equals 1 hour of haircutting. Fair and simple.",
        feature_3_title: "AI Community Guide",
        feature_3_desc: "Our Oracle understands human needs and connects neighbors who perfectly complement each other.",
        
        // Impact
        impact_title: "OUR IMPACT ON THE WORLD",
        impact_subtitle: "Every exchange on TimeAura means:",
        impact_1: "1 new real-life connection (not on Instagram)",
        impact_2: "0 carbon footprint (local service, no delivery)",
        impact_3: "1 person who felt: 'I am not alone'",
        impact_4: "1 step toward rebuilding a community",
        impact_goal_title: "By 2027 we plan to achieve:",
        impact_goal_1: "100,000+ real neighbor meetings",
        impact_goal_2: "500,000+ hours of mutual aid",
        impact_goal_3: "10,000+ restored 'lost' connections",
        impact_goal_4: "15% reduction in urban loneliness",
        impact_conclusion: "We are not just building a platform. We are building a world where 'neighbor' is not a word from the past.",

        // Business Model
        biz_title: "HOW IT WORKS?",
        biz_subtitle: "TimeAura combines a social mission with a sustainable business model.",
        biz_layer_title: "THREE-LAYER ECONOMY",
        biz_l1_t: "Fiat (USD/EUR)",
        biz_l1_d: "For professional services and B2B contracts. Stripe Connect handles payments from $1 to $13,000+.",
        biz_l2_t: "Horas (Time)",
        biz_l2_d: "For barter and neighborhood mutual aid. 1 Hora = 60 minutes of work. Zero fees.",
        biz_l3_t: "Premium (Enlightened)",
        biz_l3_d: "$4.99/month subscription. Search priority, advanced analytical tools.",
        biz_rev_title: "REVENUE STREAMS",
        biz_r1_t: "Transaction Fees",
        biz_r1_d: "7% fee on fiat transactions via Stripe. (e.g., 1,000 deals of $100 = $700 revenue).",
        biz_r2_t: "Enlightened Subscription",
        biz_r2_d: "$4.99/month for advanced functionality. 1,000 subscribers = $4,990/month.",
        biz_r3_t: "Premium Features",
        biz_r3_d: "Post bumping, profile highlighting. Microtransactions via Unity IAP.",
        biz_r4_t: "B2B Contracts",
        biz_r4_d: "10-15% commission on large deals ($1,000+). Direct enterprise integration.",
        biz_proj_title: "REVENUE PROJECTION",
        biz_p1_t: "Soft Launch (Q3 2026)",
        biz_p1_d: "1,000 users<br>~$500/month MRR",
        biz_p2_t: "Full Launch (Q4 2026)",
        biz_p2_d: "10,000 users<br>~$5,000/month MRR",
        biz_p3_t: "Scale (2027)",
        biz_p3_d: "100,000 users<br>~$50,000/month MRR",
        biz_conclusion: "We are building a sustainable business where every exchange brings value to both the platform and users.",
        
        // Company
        company_title: "Who We Are",
        company_desc: "Zarbatana Systems is an indie game development studio founded by Roman Haliulko, specializing in AI-assisted development workflows and mobile-first experiences. We build tools and platforms that empower creators.",
        portfolio_1: "Published on Google Play, Amazon, AppGallery",
        portfolio_2: "Monetization Orchestrator, Ads Utilities",
        portfolio_3: "Our flagship platform (in development)",
        
        // Tech
        tech_title: "Built with Cutting-Edge Technology",
        tech_1: "Mobile Client",
        tech_2: "Web Portal",
        tech_3: "Primary AI",
        tech_4: "Payments & Escrow",
        tech_5: "Backend & Auth",
        tech_6: "Infrastructure",
        tech_7: "LiveOps Config",
        tech_8: "Styling",

        // Metrics
        metrics_title: "OUR GOALS & KPI",
        metrics_subtitle: "We measure success not just by profit, but by real impact on people's lives.",
        m_user_title: "USER METRICS",
        m_u1_v: "1,000",
        m_u1_t: "Active Users (Soft Launch, Q3 2026)",
        m_u2_v: "10,000",
        m_u2_t: "Active Users (Full Launch, Q4 2026)",
        m_u3_v: "100,000",
        m_u3_t: "Active Users (Scale, 2027)",
        m_soc_title: "SOCIAL IMPACT",
        m_s1_v: "50,000+",
        m_s1_t: "Real neighbor meetings (by 2027)",
        m_s2_v: "200,000+",
        m_s2_t: "Hours of mutual aid (by 2027)",
        m_s3_v: "15%",
        m_s3_t: "Reduction in loneliness in partner cities",
        m_fin_title: "FINANCIAL METRICS",
        m_f1_v: "$500/mo",
        m_f1_t: "MRR (Soft Launch)",
        m_f2_v: "$5,000/mo",
        m_f2_t: "MRR (Full Launch)",
        m_f3_v: "$50,000/mo",
        m_f3_t: "MRR (Scale 2027)",
        m_geo_title: "GEO EXPANSION",
        m_g1: "2026: Ukraine (Kyiv, Lviv, Odesa, Dnipro)",
        m_g2: "2027: Poland, Germany",
        m_g3: "2028: EU, USA",
        metrics_conclusion: "We start in Ukraine, but we are building a global platform.",
        
        // Why Now
        whynow_title: "WHY NOW?",
        whynow_1: '<strong class="text-white">2020-2024:</strong> The pandemic showed us how lonely we are.',
        whynow_2: '<strong class="text-white">2025:</strong> AI reached a point where it can deeply understand human needs.',
        whynow_3: '<strong class="text-white">2026:</strong> People are ready to return to the real world.',
        whynow_4: '<strong class="text-gold">2027:</strong> TimeAura will become the standard of living not only in cities, but also in towns and villages.',
        whynow_conclusion: "We are on the verge of a new era. An era where technology serves not to escape reality, but to return to it. Those who invest now, shape the future.",
        
        // Roadmap
        roadmap_title: "Our Journey",
        road_1: "Core Architecture, AI Integration",
        road_2: "Three-Layer Economy, Commerce Orchestrator",
        road_3: "Soft Launch (Mobile, 1k users)",
        road_4: "Web Portal Launch, B2B Features",
        road_5: "Creator Platform, 100k+ users",
        q1: "Q1 2026",
        q2: "Q2 2026",
        q3: "Q3 2026",
        q4: "Q4 2026",
        
        // Investors
        inv_prop_title: "INVESTMENT PROPOSAL",
        inv_prop_subtitle: "TimeAura is a unique opportunity to invest in the future of social economies at an early stage.",
        inv_market_title: "MARKET OPPORTUNITY",
        inv_m1_v: "$1.5T",
        inv_m1_t: "Global freelance market (2026)",
        inv_m2_v: "34%",
        inv_m2_t: "Annual gig-economy growth",
        inv_m3_v: "87%",
        inv_m3_t: "City dwellers seeking local communities",
        inv_adv_title: "COMPETITIVE ADVANTAGES",
        inv_a1_t: "Hybrid Model",
        inv_a1_d: "We combine barter + fiat + subscriptions. Competitors offer only one.",
        inv_a2_t: "AI Integration",
        inv_a2_d: "Qwen by Alibaba Cloud provides personalized neighbor matching.",
        inv_a3_t: "Local Focus",
        inv_a3_d: "Not just 'another freelance board'. We build physical communities.",
        inv_a4_t: "Ready Architecture",
        inv_a4_d: "Already integrated: Stripe, Firebase, Alibaba Cloud, Unity IAP.",
        inv_ask_title: "WHAT WE ARE LOOKING FOR",
        inv_ask_1: '<strong class="text-white text-xl">Seed Round: $150,000</strong><br>To complete mobile app development and launch Soft Launch (Q3 2026).',
        inv_ask_2: '<strong class="text-cyan">Use of Funds:</strong><br>40% Development (mobile + web)<br>30% Marketing & User Acquisition<br>20% Infrastructure (Alibaba Cloud)<br>10% Operational Expenses',
        inv_ask_3: '<strong class="text-gold">ROI Projection:</strong><br>18-24 months to break-even<br>5x return on investment by 2028',
        inv_btn_deck: "📄 Technical Architecture",
        inv_btn_call: "💬 Telegram",
        inv_conclusion: "We are not just looking for money. We are looking for partners who believe in the power of communities and social innovation.",
        
        contact_title: "Get In Touch",
        contact_email: "Email",
        contact_tg: "Telegram",
        contact_location: "Location",
        contact_loc_text: "Boryslav, Ukraine (Remote)",
        footer_rights: "© 2026 Zarbatana Systems. All rights reserved.",
        footer_powered: "Powered by Alibaba Cloud • Built with Unity & React"
    },
    ua: {
        nav_about: "Про нас",
        nav_company: "Компанія",
        nav_tech: "Технології",
        nav_roadmap: "Дорожня карта",
        nav_investors: "Інвесторам",
        nav_contact: "Контакти",
        
        hero_subtitle: "Повертаємо справжнє живе спілкування",
        hero_desc: "Відірвіться від екранів. Ми використовуємо AI не для того, щоб тримати вас онлайн, а щоб об'єднати з людьми поруч. Бо справжні зв'язки народжуються в реальному світі.",
        btn_tech_doc: "Технічна Архітектура",
        btn_contact: "Зв'язатися",
        
        manifesto_title: "НАШ МАНІФЕСТ",
        manifesto_1: "Сусід — це не той, хто за стіною. Це той, хто поруч, коли потрібно.",
        manifesto_2: "Час цінніший за гроші. Бо його не можна заробити знову.",
        manifesto_3: "Найкращий алгоритм — це людська емпатія. AI лише допомагає знайти.",
        manifesto_4: "Справжня магія — не в екрані. Вона в очах людини, якій ти допоміг.",
        manifesto_conclusion: "TimeAura — це не додаток. Це рух за повернення до себе. До людей. До життя.",
        
        problem_title: "ЕПОХА САМОТНЬОСТІ",
        problem_subtitle: "Ми живемо в час парадоксу:",
        problem_1: "87% міських жителів не знають імен сусідів по під'їзду",
        problem_2: "Кожна 3-тя людина відчуває хронічну самотність",
        problem_3: "Ми маємо 500 друзів у Facebook, але нікого, хто допоможе пересунути диван",
        problem_conclusion: "Інтернет з'єднав континенти, але роз'єднав поверхи. Ми втратили те, що наші діди мали природньо — спільноту, де кожен знав кожного.",
        
        about_title: "Що таке TimeAura?",
        about_desc: "TimeAura — це платформа, покликана відновити культуру живої взаємодопомоги, яка існувала до епохи інтернету. Професійний кухар та талановитий перукар можуть жити на одному поверсі й не знати, що один мріє навчитися готувати піцу, а іншому потрібна крута стрижка. TimeAura руйнує ці стіни. Наш передовий штучний інтелект бере на себе всю оркестрацію платформи — він передбачає потреби, керує пошуком співпадінь, відстежує обміни та автоматично з'єднує сусідів, які ідеально доповнюють одне одного. Ми перетворюємо час на валюту доброти, дозволяючи сусідам у межах району чи міста обмінюватися послугами, допомагати одне одному та будувати справжні, фізичні спільноти.",
        
        // Story
        story_title: "УЯВІТЬ СОБІ...",
        story_p1: "Марія, 34 роки, Київ. Живе на 5-му поверсі вже 3 роки. Вона — перукар. Чудово стрижє, але мріє навчитися готувати італійську піцу.",
        story_p2: "На 6-му поверсі живе Андрій. Шеф-кухар. Готує найкращу карбонару в місті. Але йому давно потрібна стрижка — він соромиться йти в салон.",
        story_p3: "Вони живуть на відстані 3 метрів. Але ніколи не говорили.",
        story_p4: "TimeAura з'єднав їх за 4 секунди. ШІ-Оракул побачив: «Перукар поруч хоче піцу. Шеф поруч хоче стрижку».",
        story_p5: "Сьогодні Марія вчить Андрія готувати тісто. Андрій стрижє Марію в суботу вранці. Вони п'ють каву на балконі. Вони — друзі.",
        story_conclusion: "Це не магія. Це сусідство, яке ми забули.",

        earn_title: "ЯК ЗАРОБЛЯТИ НА TIMEAURA?",
        earn_subtitle: "Платформа відкриває кілька шляхів доходу для активних учасників спільноти.",
        earn_freelance_title: "ДЛЯ ФРІЛАНСЕРІВ",
        earn_f1_t: "Прямі замовлення",
        earn_f1_d: "Отримуй фіатні платежі за професійні послуги (від $10 до $13,000+).",
        earn_f2_t: "Бартер навичок",
        earn_f2_d: "Обмінюй свої послуги на послуги інших. Економ гроші, отримуй потрібне.",
        earn_f3_t: "Побудова репутації",
        earn_f3_d: "Кожна успішна угода підвищує твій рейтинг. Більше замовлень = більший дохід.",
        earn_neighbor_title: "ДЛЯ СУСІДІВ",
        earn_n1_t: "Локальна економіка",
        earn_n1_d: "Заробляй Хори, допомагаючи сусідам. Витрачай їх на послуги поруч.",
        earn_n2_t: "Мікро-послуги",
        earn_n2_d: "Навіть 30 хвилин допомоги = цінність. Пересунути меблі, вигуляти собаку, допомогти з комп'ютером.",
        earn_n3_t: "Спільнота",
        earn_n3_d: "Будуй довіру в своєму районі. Стань «сусідом, на якого можна покластися».",
        earn_pro_title: "ДЛЯ ПРОФЕСІОНАЛІВ",
        earn_p1_t: "Enlightened підписка",
        earn_p1_d: "Пріоритет в пошуку, розширена аналітика, VIP-підтримка.",
        earn_p2_t: "B2B контракти",
        earn_p2_d: "Працюй з компаніями напряму. TimeAura забезпечує ескроу та безпеку.",
        earn_p3_t: "Масштабування",
        earn_p3_d: "Від індивідуальних замовлень до постійних клієнтів.",
        earn_story_title: "Приклад: Олена, веб-дизайнер",
        earn_story_1: "Місяць 1: 3 замовлення через TimeAura = $450",
        earn_story_2: "Місяць 3: 8 замовлень + 2 постійних клієнти = $1,200",
        earn_story_3: "Місяць 6: Власна студія, 3 помічники = $3,500/місяць",
        earn_conclusion: "TimeAura — це не просто додаток. Це твій шлях до фінансової незалежності.",
        
        feature_1_title: "Жива взаємодопомога",
        feature_1_desc: "Знаходьте людей у своєму районі для реального, фізичного обміну послугами віч-на-віч.",
        feature_2_title: "Час як валюта",
        feature_2_desc: "Обмінюйтесь навичками за допомогою Хор. 1 година уроку кулінарії дорівнює 1 годині стрижки.",
        feature_3_title: "AI-Провідник",
        feature_3_desc: "Наш Оракул розуміє людські потреби та з'єднує сусідів, які ідеально доповнюють одне одного.",
        
        impact_title: "НАШ ВПЛИВ НА СВІТ",
        impact_subtitle: "Кожен обмін на TimeAura — це:",
        impact_1: "1 нове реальне знайомство (не в Instagram)",
        impact_2: "0 вуглецевого сліду (послуга поруч, не доставка)",
        impact_3: "1 людина, яка відчула: «Я не сам»",
        impact_4: "1 крок до відновлення спільноти",
        impact_goal_title: "До 2027 ми плануємо:",
        impact_goal_1: "100,000+ реальних зустрічей сусідів",
        impact_goal_2: "500,000+ годин живої взаємодопомоги",
        impact_goal_3: "10,000+ відновлених «втрачених» зв'язків",
        impact_goal_4: "Зниження рівня самотності в містах на 15%",
        impact_conclusion: "Ми не просто будуємо платформу. Ми будуємо світ, де «сусід» — це не слово з минулого.",

        biz_title: "ЯК ЦЕ ПРАЦЮЄ?",
        biz_subtitle: "TimeAura поєднує соціальну місію зі сталою бізнес-моделлю.",
        biz_layer_title: "ТРИШАРОВА ЕКОНОМІКА",
        biz_l1_t: "Фіат (USD/EUR)",
        biz_l1_d: "Для професійних послуг та B2B контрактів. Stripe Connect обробляє платежі від $1 до $13,000+.",
        biz_l2_t: "Хори (Час)",
        biz_l2_d: "Для бартеру та сусідської взаємодопомоги. 1 Хора = 60 хвилин роботи. Без комісій.",
        biz_l3_t: "Преміум (Enlightened)",
        biz_l3_d: "Підписка $4.99/місяць. Пріоритет в пошуку, розширені аналітичні інструменти.",
        biz_rev_title: "ДЖЕРЕЛА ДОХОДУ",
        biz_r1_t: "Комісія з угод",
        biz_r1_d: "7% з фіатних транзакцій через Stripe. При 1,000 угод на $100 = $700 доходу.",
        biz_r2_t: "Підписка Enlightened",
        biz_r2_d: "$4.99/місяць за розширений функціонал. 1,000 підписників = $4,990/місяць.",
        biz_r3_t: "Преміум-функції",
        biz_r3_d: "Підняття постів, виділення профілю. Мікротранзакції через Unity IAP.",
        biz_r4_t: "B2B контракти",
        biz_r4_d: "Комісія 10-15% на великих угодах ($1,000+). Пряма інтеграція з компаніями.",
        biz_proj_title: "ПРОГНОЗ ДОХОДІВ",
        biz_p1_t: "Soft Launch (Q3 2026)",
        biz_p1_d: "1,000 користувачів<br>~$500/місяць MRR",
        biz_p2_t: "Full Launch (Q4 2026)",
        biz_p2_d: "10,000 користувачів<br>~$5,000/місяць MRR",
        biz_p3_t: "Scale (2027)",
        biz_p3_d: "100,000 користувачів<br>~$50,000/місяць MRR",
        biz_conclusion: "Ми будуємо стійкий бізнес, де кожен обмін приносить цінність і платформі, і користувачам.",
        
        company_title: "Хто Ми",
        company_desc: "Zarbatana Systems — це інді-студія розробки ігор, заснована Романом Галіульком, що спеціалізується на робочих процесах з підтримкою AI та mobile-first продуктах. Ми створюємо інструменти та платформи, які надихають творців.",
        portfolio_1: "Опубліковано в Google Play, Amazon, AppGallery",
        portfolio_2: "Monetization Orchestrator, Ads Utilities",
        portfolio_3: "Наша головна платформа (в розробці)",
        
        tech_title: "Створено за передовими технологіями",
        tech_1: "Мобільний Клієнт",
        tech_2: "Веб-портал",
        tech_3: "Основний AI",
        tech_4: "Платежі та Ескроу",
        tech_5: "Бекенд та Авторизація",
        tech_6: "Інфраструктура",
        tech_7: "LiveOps Конфігурація",
        tech_8: "Стилізація",

        metrics_title: "НАШІ ЦІЛІ ТА KPI",
        metrics_subtitle: "Ми вимірюємо успіх не тільки прибутком, а й реальним впливом на життя людей.",
        m_user_title: "КОРИСТУВАЦЬКІ МЕТРИКИ",
        m_u1_v: "1,000",
        m_u1_t: "Активних користувачів (Soft Launch, Q3 2026)",
        m_u2_v: "10,000",
        m_u2_t: "Активних користувачів (Full Launch, Q4 2026)",
        m_u3_v: "100,000",
        m_u3_t: "Активних користувачів (Scale, 2027)",
        m_soc_title: "СОЦІАЛЬНИЙ ВПЛИВ",
        m_s1_v: "50,000+",
        m_s1_t: "Реальних зустрічей сусідів (до 2027)",
        m_s2_v: "200,000+",
        m_s2_t: "Годин взаємодопомоги (до 2027)",
        m_s3_v: "15%",
        m_s3_t: "Зниження самотності в містах-партнерах",
        m_fin_title: "ФІНАНСОВІ ПОКАЗНИКИ",
        m_f1_v: "$500/міс",
        m_f1_t: "MRR (Soft Launch)",
        m_f2_v: "$5,000/міс",
        m_f2_t: "MRR (Full Launch)",
        m_f3_v: "$50,000/міс",
        m_f3_t: "MRR (Scale 2027)",
        m_geo_title: "GEO РОЗШИРЕННЯ",
        m_g1: "2026: Україна (Київ, Львів, Одеса, Дніпро)",
        m_g2: "2027: Польща, Німеччина",
        m_g3: "2028: ЄС, США",
        metrics_conclusion: "Ми починаємо з України, але будуємо глобальну платформу.",
        
        whynow_title: "ЧОМУ САМЕ ЗАРАЗ?",
        whynow_1: '<strong class="text-white">2020-2024:</strong> Пандемія показала, наскільки ми самотні.',
        whynow_2: '<strong class="text-white">2025:</strong> AI досяг точки, де може розуміти людські потреби.',
        whynow_3: '<strong class="text-white">2026:</strong> Люди готові повернутися до реального світу.',
        whynow_4: '<strong class="text-gold">2027:</strong> TimeAura стане стандартом життя не лише в містах, але й у селах та містечках.',
        whynow_conclusion: "Ми на порозі нової епохи. Епохи, де технологія слугує не втечі від реальності, а поверненню до неї. Хто інвестує зараз — формує майбутнє.",
        
        roadmap_title: "Наш Шлях",
        road_1: "Базова архітектура, Інтеграція AI",
        road_2: "Тришарова Економіка, Commerce Orchestrator",
        road_3: "Soft Launch (Мобільний, 1к користувачів)",
        road_4: "Запуск веб-порталу, B2B функціонал",
        road_5: "Платформа для творців, 100k+ користувачів",
        q1: "1-й Квартал 2026",
        q2: "2-й Квартал 2026",
        q3: "3-й Квартал 2026",
        q4: "4-й Квартал 2026",
        
        inv_prop_title: "ІНВЕСТИЦІЙНА ПРОПОЗИЦІЯ",
        inv_prop_subtitle: "TimeAura — це унікальна можливість інвестувати в майбутнє соціальних економік на ранній стадії.",
        inv_market_title: "РИНКОВА МОЖЛИВІСТЬ",
        inv_m1_v: "$1.5T",
        inv_m1_t: "Глобальний ринок фрілансу (2026)",
        inv_m2_v: "34%",
        inv_m2_t: "Щорічний ріст gig-економіки",
        inv_m3_v: "87%",
        inv_m3_t: "Міських жителів шукають локальні спільноти",
        inv_adv_title: "КОНКУРЕНТНІ ПЕРЕВАГИ",
        inv_a1_t: "Гібридна модель",
        inv_a1_d: "Поєднуємо бартер + фіат + підписки. Конкуренти пропонують лише одне.",
        inv_a2_t: "AI-інтеграція",
        inv_a2_d: "Qwen від Alibaba Cloud забезпечує персоналізований matching сусідів.",
        inv_a3_t: "Локальний фокус",
        inv_a3_d: "Не просто «ще одна біржа фрілансу». Ми будуємо фізичні спільноти.",
        inv_a4_t: "Готова архітектура",
        inv_a4_d: "Вже інтегровані: Stripe, Firebase, Alibaba Cloud, Unity IAP.",
        inv_ask_title: "ЩО МИ ШУКАЄМО",
        inv_ask_1: '<strong class="text-white text-xl">Посівний раунд (Seed): $150,000</strong><br>Для завершення розробки мобільного додатку та запуску Soft Launch (Q3 2026).',
        inv_ask_2: '<strong class="text-cyan">Розподіл коштів:</strong><br>40% Розробка (мобільний + веб)<br>30% Маркетинг та залучення користувачів<br>20% Інфраструктура (Alibaba Cloud)<br>10% Операційні витрати',
        inv_ask_3: '<strong class="text-gold">Прогноз окупності (ROI):</strong><br>18-24 місяці до break-even<br>5x повернення інвестицій до 2028',
        inv_btn_deck: "📄 Технічна Архітектура",
        inv_btn_call: "💬 Написати в Telegram",
        inv_conclusion: "Ми шукаємо не просто гроші. Ми шукаємо партнерів, які вірять у силу спільнот та соціальних інновацій.",
        
        contact_title: "Зв'яжіться З Нами",
        contact_email: "Пошта",
        contact_tg: "Телеграм",
        contact_location: "Локація",
        contact_loc_text: "Борислав, Україна (Віддалено)",
        footer_rights: "© 2026 Zarbatana Systems. Всі права захищено.",
        footer_powered: "Працює на Alibaba Cloud • Створено на Unity та React"
    }
};

// Initial state
let currentLang = localStorage.getItem('lang') || 'en';

// Apply translations on load
document.addEventListener('DOMContentLoaded', () => {
    AOS.init({
        duration: 800,
        once: true,
        offset: 100
    });
    
    initParticles();
    setLanguage(currentLang);
});

// Toggle Language
function toggleLang() {
    currentLang = currentLang === 'en' ? 'ua' : 'en';
    localStorage.setItem('lang', currentLang);
    setLanguage(currentLang);
}

// Apply Translation
function setLanguage(lang) {
    // Update button text
    const langBtn = document.getElementById('lang-toggle');
    if(langBtn) {
        langBtn.innerText = lang === 'en' ? '🇺🇦' : '🇬🇧';
    }

    // Apply texts
    const elements = document.querySelectorAll('[data-i18n]');
    elements.forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (translations[lang] && translations[lang][key]) {
            el.innerHTML = translations[lang][key];
        }
    });
    
    document.documentElement.lang = lang;
}

// Particles.js Initialization
function initParticles() {
    if(typeof particlesJS !== 'undefined') {
        particlesJS('particles-js', {
            "particles": {
                "number": { "value": 60, "density": { "enable": true, "value_area": 800 } },
                "color": { "value": ["#d4af37", "#4a90e2"] },
                "shape": { "type": "circle" },
                "opacity": { "value": 0.5, "random": true },
                "size": { "value": 3, "random": true },
                "line_linked": { "enable": true, "distance": 150, "color": "#4a90e2", "opacity": 0.2, "width": 1 },
                "move": { "enable": true, "speed": 2, "direction": "none", "random": true, "out_mode": "out" }
            },
            "interactivity": {
                "detect_on": "canvas",
                "events": {
                    "onhover": { "enable": true, "mode": "grab" },
                    "onclick": { "enable": true, "mode": "push" }
                },
                "modes": {
                    "grab": { "distance": 140, "line_linked": { "opacity": 0.5 } }
                }
            },
            "retina_detect": true
        });
    }
}
