# Ticket System + .NET Backend

Repository layout:

- `ticket-system/` -> existing HTML/CSS/JS frontend
- `backend/` -> ASP.NET Core API that serves the frontend and exposes `/api/*`

Run the app from the repository root:

```bash
dotnet run --project backend/TicketSystem.Api.csproj
```

Then open `http://localhost:5000`.

If you open the frontend files directly from disk, the app still works in the original `localStorage` mode.
