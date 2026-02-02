@echo off
echo ========================================
echo  Running CryptoFileExchange Test Suite
echo ========================================
echo.

dotnet run --project CryptoFileExchange.csproj -- --test

echo.
echo ========================================
pause
