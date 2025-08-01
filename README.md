🤖 Telegram-бот для продажу автострахування
Цей бот допомагає користувачам купити автостраховку у Telegram, крок за кроком.

Використовує:

Mindee API для розпізнавання документів (паспорт, техпаспорт)

OpenAI GPT-3.5 для "розумної" взаємодії з користувачем

.NET 9 і Telegram.Bot

⚙️ Функціонал
/start — початок розмови, привітання і пояснення

Користувач надсилає фото паспорта (передня і задня сторони)

Користувач надсилає фото техпаспорта

Обробка фото через Mindee API — отримання даних

Підтвердження користувачем правильності даних (так/ні)

Фіксована ціна страховки 100$ — підтвердження згоди

Генерація та відправка фіктивного страхового полісу (.txt)

Всі інші повідомлення обробляються OpenAI для "розумної" комунікації

🧰 Технології
.NET 9 (C#)

Telegram.Bot

Mindee API

OpenAI API (ChatCompletion)

Dependency Injection

Serilog (логування)

🚀 Як запустити
Клонувати репозиторій:

git clone https://github.com/BlackBear75/Private_Test_Mindee_API.git
cd Private_Test_Mindee_API
Додати appsettings.json у корінь проекту (приклад):

{
  "TelegramBotToken": "your_telegram_bot_token_here",
  "OpenAI": {
    "ApiKey": "your_openai_api_key_here"
  },
  "Mindee": {
    "ApiKey": "your_mindee_api_key_here"
  }
}

Запустити:
dotnet run


📝 Робочий процес бота
Користувач надсилає /start

Бот просить фото паспорта (передня частина)

Бот просить фото паспорта (задня частина)

Бот виконує розпізнавання (Mindee API або мок)

Показує розпізнані дані, питає "Чи правильно?"

Якщо користувач "ні" — бот просить повторити фото

Якщо "так" — бот просить фото техпаспорта

Аналогічна обробка техпаспорта, підтвердження

Бот повідомляє ціну 100 USD, питає згоду

Якщо "так" — бот генерує страховий поліс і надсилає файл

Якщо "ні" — бот пояснює, що ціна фіксована

Всі інші повідомлення відповідає через OpenAI


Користувач: /start  
Бот: Привіт! Надішліть фото паспорта (передню частину) 📷  
Користувач: [фото]  
Бот: ✅ Передня сторона паспорта отримана. Надішліть задню сторону.  
Користувач: [фото]  
Бот: 👤 Розпізнано паспорт: Ім’я: Богдан ... Чи правильно? (так / ні)  
Користувач: так  
Бот: Добре, надішліть фото техпаспорта.  
Користувач: [фото]  
Бот: 🚗 Розпізнано Техпаспорт: VIN: ... Чи правильно? (так / ні)  
Користувач: так  
Бот: Ціна страховки — 100 USD 💵. Згодні? (так / ні)  
Користувач: так  
Бот: ✅ Ось ваш страховий поліс [файл]  
