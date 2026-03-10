# CryptoFileExchange

A C#/.NET desktop application (WinForms) for exchanging files using cryptographic operations.

> Repo: `dusanvelickovic/CryptoFileExchange`

## What's in this repository

At a high level, the repo contains:

- **WinForms UI** entry points:
  - `Program.cs`
  - `Form1.cs` (+ `Form1.Designer.cs`, `Form1.resx`)
- Project/solution files:
  - `CryptoFileExchange.csproj`
  - `CryptoFileExchange.slnx`
- Code organized into folders:
  - `Algorithms/` – cryptographic algorithm implementations/helpers
  - `Models/` – DTOs / domain models
  - `Services/` – application services (crypto, file operations, etc.)
  - `UI/` – additional UI components/forms
  - `Tests/` – tests

## Requirements

- Windows (WinForms)
- .NET SDK (recommended: latest LTS)

## Build & Run

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run
```

Or open the solution/project in **Visual Studio** and run the WinForms app.

## Testing

If the `Tests/` project is configured:

```bash
dotnet test
```

## Usage (high-level)

1. Launch the application.
2. Select/prepare a file to exchange.
3. Choose the cryptographic option/algorithm (if exposed in the UI).
4. Perform the operation (encrypt/sign/etc.) and share the resulting file as needed.

> Note: The exact workflow depends on what options are exposed in `Form1` and under `UI/`.

## Security notes

- Treat this as a learning/demo project unless you have reviewed the cryptography carefully.
- Prefer modern primitives and authenticated encryption modes.
- Avoid reusing keys/IVs and ensure secure random generation.

## Contributing

PRs and issues are welcome. If you plan to contribute:

- Keep changes small and focused
- Add/adjust tests when changing crypto logic
- Update this README when behavior changes

## License

No license file is currently included in the repository.  
If you want this to be open-source, add a `LICENSE` file (MIT/Apache-2.0/GPL/etc.).
