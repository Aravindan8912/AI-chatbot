# Tree AI Chatbot

Full-stack AI chatbot with:
- Angular frontend (`Frontend`)
- ASP.NET Core Web API backend (`Backend`)
- OpenAI Chat Completions integration
- Serilog-based request and application logging

## Tech Stack

### Frontend
- Angular 21 standalone components
- RxJS for request timeout and stream handling
- Angular HTTP interceptor for client-side request logging
- Dev proxy (`/api` -> `http://localhost:5000`) to avoid CORS issues

### Backend
- ASP.NET Core (.NET 10)
- Clean layered structure:
  - `Api` (controllers + host config)
  - `Application` (chat business logic)
  - `Infrastructure` (OpenAI client)
- Serilog for terminal and rolling file logs

## Project Structure

```text
.
├─ Backend/
│  ├─ Api/
│  │  ├─ Controllers/ChatController.cs
│  │  ├─ Program.cs
│  │  └─ EnvFileLocator.cs
│  ├─ Application/
│  │  ├─ DTOs/
│  │  ├─ Interfaces/
│  │  └─ Services/ChatService.cs
│  ├─ Infrastructure/
│  │  └─ OpenAI/OpenAIClient.cs
│  ├─ .env.example
│  └─ ChatBot.sln
└─ Frontend/
   ├─ src/app/
   │  ├─ features/chat/
   │  ├─ core/logging/
   │  └─ shared/ui/
   ├─ src/environments/
   ├─ angular.json
   └─ proxy.conf.json
```

## End-to-End Request Flow

1. User types a message in frontend composer.
2. Frontend `ChatService` sends `POST /api/chat` (dev: proxied through Angular to backend).
3. Backend `ChatController` validates input.
4. Backend `ChatService`:
   - Returns a local friendly reply for casual greetings (`hi`, `hello`, `hai`, `hey`, etc.).
   - Otherwise builds OpenAI `messages` payload.
5. `OpenAIClient` calls `https://api.openai.com/v1/chat/completions`.
6. Backend returns `{ response: "..." }`.
7. Frontend appends assistant message to message list and updates UI.
8. Logs are written:
   - Frontend: browser console (`[frontend] ...`)
   - Backend: terminal + `Backend/Api/logs/api-*.log`

## Environment Configuration

### Backend
Copy and edit env file:

```bash
cd Backend
copy .env.example .env
```

Set values in `Backend/.env`:

```env
OPENAI_API_KEY=your_openai_api_key_here
OPENAI_MODEL=gpt-4o-mini
```

> Never commit real API keys.

### Frontend
`src/environments/environment.ts` uses:

```ts
apiUrl: '/api'
```

In development, Angular proxy forwards `/api` to `http://localhost:5000`.

## Run Locally

### 1) Start Backend

```bash
cd Backend/Api
dotnet run
```

Backend default URL:
- `http://localhost:5000`

Swagger:
- `http://localhost:5000/swagger`

### 2) Start Frontend

```bash
cd Frontend
npm install
npm start
```

Frontend URL:
- `http://localhost:4200`

## API Contract

### `POST /api/chat`

Request:

```json
{
  "message": "hello"
}
```

Success response:

```json
{
  "response": "Hey buddy! How can I help make your day better?"
}
```

Common errors:
- `400`: empty message
- `503`: API key missing
- `502`: upstream OpenAI API failure

## Logging

### Backend (Serilog)
- Request logs: method, route, status, duration
- Chat service logs: request length, greeting classification, OpenAI response state
- Sink outputs:
  - Console
  - Rolling file: `Backend/Api/logs/api-YYYYMMDD.log`

### Frontend
- HTTP interceptor logs all request/response/error events
- Chat component logs send, parse, timeout, finalize states

## Security Notes

- `Backend/.env` is gitignored and must stay local.
- Keep `Backend/.env.example` with placeholders only.
- If a key was pushed previously, rotate/revoke it in OpenAI dashboard and rewrite git history before pushing.

## Build Commands

### Backend

```bash
dotnet build Backend/Api/Api.csproj
```

### Frontend

```bash
cd Frontend
npm run build
```

## Troubleshooting

- **Frontend stuck on "Thinking..."**
  - Confirm backend logs show `POST /api/chat` returning `200`.
  - Check browser console for `[frontend]` logs.
  - Restart `ng serve` and hard refresh (`Ctrl+F5`).

- **CORS / OPTIONS issues**
  - Use frontend dev proxy (`/api`) and run via `http://localhost:4200`.
  - Ensure backend is listening on `http://localhost:5000`.

- **Push blocked by GitHub secret scanning**
  - Remove real key from all tracked files and commit history.
  - Rotate leaked key immediately.
