# Lab 1 — потокобезпечна колекція (C#, .NET 8)

Запуск:
```
cd Lab1
dotnet run -c Release
```
Файли:
* `ICustomStorage.cs` — узагальнений інтерфейс з завдання
* `DynamicArrayStorageImplementation.cs` — власна колекція (масив, фільтр, LIFO, події, перемикач `enableLock`, `lock (_syncRoot)`)
* `Program.cs` — 3 продюсери, потік-монітор (`IsBackground = true`), 3 демонстрації
