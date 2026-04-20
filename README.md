# ⚡ VitalTrack — Universal Health & Fitness Tracker

Production-ready **Windows Desktop WPF application** (.NET 8) backed by **Microsoft SQL Server**, implementing strict **3-Tier Architecture** with RBAC, gender-aware health calculations, external API integrations, and a floating AI chatbot.

---

## 🏗 3-Tier Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  PRESENTATION TIER  —  VitalTrack.UI  (WPF / XAML)          │
│  LoginWindow · MainWindow · DashboardPage · ProfilePage      │
│  WorkoutsPage · NutritionPage · AdminPage                    │
│  LogWorkoutDialog · LogMealDialog (code-only dialog)         │
│  Styles: Colors.xaml · Controls.xaml                        │
├──────────────────────────────────────────────────────────────┤
│  BUSINESS LOGIC TIER  —  VitalTrack.Business  (C#)          │
│  AuthService · HealthCalculationService · UserService        │
│  WorkoutService · NutritionService · ChatbotService          │
│  DTOs · IServices interfaces                                 │
├──────────────────────────────────────────────────────────────┤
│  DATA TIER  —  VitalTrack.Data  (EF Core + MS SQL Server)   │
│  User · WorkoutSession · NutritionLog · ChatLog entities     │
│  VitalTrackDbContext · InitialCreate migration               │
└──────────────────────────────────────────────────────────────┘
```

---

## ✅ Features

| Feature | Details |
|---|---|
| Custom borderless WPF window | Drag, minimize, close; dark purple/pink theme |
| Login / Register | BCrypt hashing, username uniqueness, age/password validation |
| Separate `Username` & `FullName` | Login ID ≠ display name |
| Gender field | Drives ALL calculations and suggestions |
| RBAC — User vs Admin | Admin secret code via `.env` |
| Gender-aware BMR | Mifflin-St Jeor: Male +5, Female −161 |
| Auto BMI + TDEE | Recalculates live on every profile keystroke |
| BMI colour-coded | Green=Normal, Amber=Under, Orange=Over, Red=Obese |
| Macro goals | Gender-split: Male 30/45/25, Female 25/50/25 |
| Workout logging | MET-based calorie burn, sets/reps/weight/duration |
| Exercise suggestions | ExerciseDB API + gender-appropriate fallback list |
| Exercise card prefill | Click a suggestion → opens dialog pre-filled |
| Nutrition logging | Per meal, per day summary, remove entries |
| Food search | Open Food Facts API (free, no key) + fallback |
| Food search zero-data filter | Skips products with all-zero macros |
| Floating AI chatbot (FAB) | Bottom-right, typing indicator, chat persisted to DB |
| Chatbot system prompt | Restricted to nutrition/fitness/running only |
| Hugging Face API | Mistral-7B-Instruct via `.env` key; rule-based fallback |
| Admin panel | All users, sessions, nutrition, platform-wide stats |
| `.env` file | All secrets/keys/connection strings |
| Auto-migrate on startup | DB created/updated automatically |
| Error handling on startup | Friendly dialog if DB unreachable |

---

## 🚀 Quick Start

### Prerequisites
- **Visual Studio 2022** (v17.8+) with `.NET Desktop Development` workload
- **.NET 8 SDK**
- **SQL Server** (Express is free: https://aka.ms/sqlexpress)

### 1 — Clone & Configure
```bash
git clone https://github.com/YourName/VitalTrack.git
cd VitalTrack
cp .env.example .env
```

Edit `.env`:
```env
DB_CONNECTION_STRING=Server=localhost;Database=VitalTrackDb;Trusted_Connection=True;TrustServerCertificate=True;
HUGGINGFACE_API_KEY=hf_YOUR_KEY_HERE
EXERCISEDB_API_KEY=YOUR_RAPIDAPI_KEY_HERE
ADMIN_SECRET_CODE=VITALTRACK_ADMIN_2025
```

### 2 — Run
Open `VitalTrack.sln` → **F5**

The database is **auto-migrated on first launch** — no manual SQL needed.

### 3 — Login
| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@2025` | Admin |

Register new users from the Login screen. Admin requires the secret code from `.env`.

---

## 🧮 Health Calculations

### BMR — Mifflin-St Jeor
```
Male:   BMR = 10W + 6.25H − 5A + 5
Female: BMR = 10W + 6.25H − 5A − 161
```

### TDEE = BMR × Activity Multiplier
| Level | Multiplier |
|---|---|
| Sedentary | 1.20 |
| Lightly Active | 1.375 |
| Moderately Active | 1.55 |
| Very Active | 1.725 |
| Extra Active | 1.90 |

### Calorie Burn (MET)
`Calories = MET × weight(kg) × hours × genderFactor`  
Female genderFactor = 0.93 (lean mass adjustment)

---

## 🔑 API Keys

| API | Key Env Var | Free Tier | Notes |
|---|---|---|---|
| Hugging Face | `HUGGINGFACE_API_KEY` | Yes | https://huggingface.co/settings/tokens |
| ExerciseDB | `EXERCISEDB_API_KEY` | Yes (100 req/day) | https://rapidapi.com/justin-WFnsXH_t6/api/exercisedb |
| Open Food Facts | — | Always free | No key needed, built-in |

All APIs have full offline fallbacks — the app works without any keys.

---

## 🗄 Database Schema

```
Users            (Id PK, Username UNIQUE, FullName, PasswordHash, Email UNIQUE,
                  Gender, Role, Age, HeightCm, WeightKg, BodyFatPercent,
                  ActivityLevel, CreatedAt, UpdatedAt)

WorkoutSessions  (Id PK, UserId FK→Users, ExerciseName, MuscleGroup, Sets, Reps,
                  WeightKg, DurationMinutes, CaloriesBurned, Notes, LoggedAt)

NutritionLogs    (Id PK, UserId FK→Users, FoodName, MealType, Calories, ProteinG,
                  CarbsG, FatG, ServingGrams, LoggedAt)

ChatLogs         (Id PK, UserId FK→Users, UserMessage, BotResponse, CreatedAt)
```

All FK cascades on delete. Gender and Role stored as strings.

---

## 📁 Project Structure

```
VitalTrack/
├── .env.example              ← Copy to .env, fill in keys
├── .gitignore
├── README.md
├── VitalTrack.sln
│
├── VitalTrack.Data/          ── DATA TIER
│   ├── Entities/
│   │   ├── User.cs           (Gender, Role enums here)
│   │   ├── WorkoutSession.cs
│   │   ├── NutritionLog.cs
│   │   └── ChatLog.cs
│   ├── Migrations/
│   │   ├── 20250101000000_InitialCreate.cs
│   │   └── VitalTrackDbContextModelSnapshot.cs
│   ├── VitalTrackDbContext.cs
│   └── VitalTrack.Data.csproj
│
├── VitalTrack.Business/      ── BUSINESS LOGIC TIER
│   ├── DTOs/Dtos.cs
│   ├── Interfaces/IServices.cs
│   ├── Services/
│   │   ├── AuthService.cs           (BCrypt, env admin code, validation)
│   │   ├── HealthCalculationService.cs (gender-aware BMR/TDEE/BMI/MET)
│   │   ├── UserService.cs
│   │   ├── WorkoutService.cs        (ExerciseDB API + fallback)
│   │   ├── NutritionService.cs      (Open Food Facts API + fallback)
│   │   └── ChatbotService.cs        (HuggingFace + rule-based fallback)
│   └── VitalTrack.Business.csproj
│
└── VitalTrack.UI/            ── PRESENTATION TIER
    ├── App.xaml / App.xaml.cs       (DI container, .env, auto-migrate)
    ├── Styles/
    │   ├── Colors.xaml              (full dark purple/pink palette)
    │   └── Controls.xaml            (Button, TextBox, DataGrid, Tag styles)
    ├── Views/
    │   ├── LoginWindow.xaml/.cs     (login + register + role select)
    │   ├── MainWindow.xaml/.cs      (sidebar nav + chatbot FAB)
    │   ├── DashboardPage.xaml/.cs   (stat cards + session grid + macros)
    │   ├── ProfilePage.xaml/.cs     (live metric recalc + macro goals)
    │   ├── WorkoutsPage.xaml/.cs    (API suggestions + session log)
    │   ├── LogWorkoutDialog.xaml/.cs(exercise logging modal)
    │   ├── NutritionPage.xaml/.cs   (daily summary + meal removal)
    │   ├── LogMealDialog.cs         (food search + API + manual entry)
    │   ├── AdminPage.xaml/.cs       (global stats + all-user tables)
    └── VitalTrack.UI.csproj
```

---

## 🔐 Security Notes

- Passwords hashed with **BCrypt** (adaptive cost factor)
- Admin code read from **environment variable** — not hardcoded
- All API keys in `.env` — never committed to source control
- RBAC enforced at **service layer** — UI role check is secondary defence
- `Username` stored in lowercase for consistent lookup

---

## 📄 License
MIT — free to use, modify, and distribute.
