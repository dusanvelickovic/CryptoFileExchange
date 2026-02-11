# UPUTSTVO - CryptoFile Exchange

Detaljno uputstvo za koriš?enje P2P encrypted file transfer aplikacije sa automatskim šifrovanjem.

---

## ?? Sadržaj

1. [O Projektu](#o-projektu)
2. [Najnovije Izmene](#najnovije-izmene)
3. [Arhitektura](#arhitektura)
4. [Algoritmi Šifrovanja](#algoritmi-šifrovanja)
5. [Kako Koristiti Aplikaciju](#kako-koristiti-aplikaciju)
6. [Podešavanje Encryption Keys](#podešavanje-encryption-keys)
7. [File System Watcher (FSW)](#file-system-watcher-fsw)
8. [File Exchange (P2P Transfer)](#file-exchange-p2p-transfer)
9. [Kompatibilnost](#kompatibilnost)
10. [Testiranje](#testiranje)
11. [Troubleshooting](#troubleshooting)

---

## O Projektu

**CryptoFile Exchange** je desktop aplikacija za **bezbedno P2P prenošenje fajlova** sa automatskim šifrovanjem.

### Klju?ne Funkcionalnosti

- ?? **Troslojno šifrovanje** (Enigma ? XXTEA ? CFB)
- ?? **Dinami?ko podešavanje klju?eva** (kroz UI)
- ??? **TigerHash** za integritet podataka (192-bit)
- ?? **Automatski File System Watcher**
- ?? **P2P File Transfer** preko TCP/IP
- ?? **Metadata header** sa file info
- ?? **Serilog** strukturno logovanje

### Tehnologije

| Komponenta | Tehnologija |
|------------|-------------|
| **Framework** | .NET 10 (C# 14.0) |
| **UI** | Windows Forms |
| **Šifrovanje** | Enigma (26-letter), XXTEA, CFB Mode |
| **Hash** | TigerHash (192-bit) |
| **Logging** | Serilog (File + Console) |
| **Networking** | TCP/IP (CFTP Protocol) |

---

## ?? Najnovije Izmene

### 1. Encryption Keys UI (NOVO!)

**File Exchange** panel sada ima **Encryption Keys** sekciju:

```
???????????????????????????????????????????????????????????
? Encryption Keys (must match on both sender and receiver)?
?                                                          ?
? Enigma Key:  [MyEnigmaSecretKey2024      ]              ?
? XXTEA Key:   [XXTEAKey12345678           ]              ?
? CFB Key:     [CFBModeKey987654           ]              ?
? CFB IV:      [                           ] (empty)      ?
?                                                          ?
? [Apply Keys]                                            ?
???????????????????????????????????????????????????????????
```

**Funkcionalnost:**
- ? Podesi klju?eve PRIJE slanja/primanja fajlova
- ? **OBA peer-a moraju imati ISTE klju?eve**
- ? Klikni "Apply Keys" da primeniš izmene
- ? Klju?evi se loguju u Activity Log

### 2. Enigma Engine Refaktor

- ? **26-letter alfabeta** (A-Z only) umesto 256-byte
- ? **Base64 encoding** za binarne podatke
- ? **Trimming null bytes** iz XXTEA padding-a
- ? **Case-sensitive mode** za Base64 stringove

### 3. XXTEA Padding Fix

- ? **Simple zero-padding** (bez 0x80 marker-a)
- ? **Kompatibilan** sa drugim implementacijama
- ? **Padding ostaje u output-u** (handled by Enigma)

### 4. CFB Mode Simplifikacija

- ? **Simple for-loop** iteracija (i += BLOCK_SIZE)
- ? **Direct XXTEA.Encrypt(feedback, key)** pozivi
- ? **Zero-padding** za partial blokove
- ? **IV support** (empty string = 16 zero bytes)

### 5. Test Suite Poboljšanja

- ? Novi **DrugaAplikacijaCompatibilityTest** (byte-level dijagnostika)
- ? Uklonjeni zastareli testovi (CompatibilityDebugTest)
- ? Ažurirani svi testovi za nove API signature

---

## Arhitektura

### Slojevi Aplikacije

```
?????????????????????????????????????????????????????????
?             UI Layer (Windows Forms)                  ?
?  - FileWatcherPanel.cs                                ?
?  - FileExchangePanel.cs (+ Encryption Keys UI)        ?
?????????????????????????????????????????????????????????
                 ?
?????????????????????????????????????????????????????????
?             Services Layer                            ?
?  - FileSystemWatcherService.cs                        ?
?  - NetworkService.cs (CFTP Protocol)                  ?
?  - EncryptionService.cs (Enigma?XXTEA?CFB)            ?
?  - DecryptionService.cs (CFB?XXTEA?Enigma)            ?
?  - MetadataService.cs                                 ?
?????????????????????????????????????????????????????????
                 ?
?????????????????????????????????????????????????????????
?          Crypto Algorithms Layer                      ?
?  - EnigmaEngine.cs (26-letter, Base64)                ?
?  - XXTEAEngine.cs (zero-padding)                      ?
?  - CFBMode.cs (simple for-loop)                       ?
?  - TigerHash.cs (192-bit)                             ?
?????????????????????????????????????????????????????????
```

---

## Algoritmi Šifrovanja

### Troslojni Lanac

```
?? Original File (bytes)
    ?
?? Layer 1: Enigma (26-letter alphabet)
    • bytes ? Base64 string ? encrypt A-Z only ? UTF-8 bytes
    • Key: "MyEnigmaSecretKey2024"
    ?
?? Layer 2: XXTEA (block cipher)
    • Input: Enigma output
    • Process: 16-byte key, zero-padding (0-3 bytes)
    • Key: "XXTEAKey12345678"
    ?
?? Layer 3: CFB Mode (cipher feedback)
    • Input: XXTEA output
    • Process: CFB with XXTEA as block cipher
    • IV: "" (empty = 16 zero bytes)
    • Key: "CFBModeKey987654"
    ?
?? Encrypted Data (binary)
```

**Dešifrovanje:**
```
CFB?¹ ? XXTEA?¹ ? Enigma?¹
```

### Default Klju?evi

| Klju? | Default Vrednost | Dužina |
|-------|-----------------|--------|
| **Enigma Key** | `MyEnigmaSecretKey2024` | Bilo koja |
| **XXTEA Key** | `XXTEAKey12345678` | 16 bytes (pad/truncate) |
| **CFB Key** | `CFBModeKey987654` | 16 bytes (pad/truncate) |
| **CFB IV** | `""` (empty) | ? 16 zero bytes |

?? **VAŽNO:** Oba peer-a MORAJU imati ISTE klju?eve!

### TigerHash (192-bit)

```csharp
// Hash se ra?una NA ŠIFROVANIM podacima
byte[] encrypted = Encrypt(originalFile);
string hash = TigerHash.ComputeHash(encrypted);

// Output: 48-char HEX string
// "a1b2c3d4e5f67890abcdef1234567890abcdef1234567890"
```

**Svrha:**
- ? Detekcija tampering-a
- ? Verifikacija integriteta
- ? Potvrda da fajl nije ošte?en

---

## Kako Koristiti Aplikaciju

### Prvi Pokretanje

```bash
dotnet build
dotnet run
```

**Automatski kreirani folderi:**
```
CryptoFileExchange.exe
??? EncryptedFiles/    # FSW output (.cfex)
??? Received/          # P2P primljeni fajlovi
??? Logs/              # Serilog (app-YYYYMMDD.log)
```

---

## Podešavanje Encryption Keys

### Zašto Je Važno?

**OBA peer-a MORAJU koristiti ISTE klju?eve!**

### Koraci

#### 1?? Otvori File Exchange Tab

#### 2?? Podesi Klju?eve

```
???????????????????????????????????????????????????????????
? Encryption Keys                                          ?
?                                                          ?
? Enigma Key:  [MyEnigmaSecretKey2024      ]              ?
? XXTEA Key:   [XXTEAKey12345678           ]              ?
? CFB Key:     [CFBModeKey987654           ]              ?
? CFB IV:      [                           ]              ?
?                                                          ?
? [Apply Keys]  ? Klikni nakon izmena                     ?
???????????????????????????????????????????????????????????
```

**Šta se dešava:**
1. Unesi nove klju?eve
2. Klikni **"Apply Keys"**
3. Servisi se reinicijalizuju
4. MessageBox potvr?uje izmenu
5. Klju?evi se loguju

#### 3?? Sinhronizuj sa Drugim Peer-om

**KRITI?NO:** Kopiraj ISTE klju?eve na drugi ra?unar!

**Ra?unar A (Receiver):**
```
Enigma: TestKey123
XXTEA:  AAAAAAAAAAAAAAAA
CFB:    BBBBBBBBBBBBBBBB
IV:     (empty)
[Apply Keys]
```

**Ra?unar B (Sender):**
```
Enigma: TestKey123       ? ISTI!
XXTEA:  AAAAAAAAAAAAAAAA ? ISTI!
CFB:    BBBBBBBBBBBBBBBB ? ISTI!
IV:     (empty)          ? ISTI!
[Apply Keys]
```

? Sada mogu da komuniciraju!

---

## File System Watcher (FSW)

### Šta je FSW?

Automatski šifruje nove fajlove u pra?enom folderu.

### Koraci

1. **Browse** ? izaberi folder
2. **Start FSW**
3. **Dodaj fajl** u folder ? automatski se šifruje!

**Output:**
```
EncryptedFiles/
??? document.pdf.cfex  # Šifrovani + metadata
```

---

## File Exchange (P2P Transfer)

### Scenario: LAN Transfer

#### Ra?unar A (Receiver):

```
1. Podesi KLJU?EVE
2. [Apply Keys]
3. Port: 9999
4. [Start Server]
```

#### Ra?unar B (Sender):

```
1. Podesi ISTE KLJU?EVE
2. [Apply Keys]
3. [Browse] ? file
4. IP: 192.168.1.100  ? Receiver IP
5. Port: 9999
6. [Send File]
```

### Šta se Dešava

```
Sender                          Receiver
??????                          ????????
1. Load file                    1. Listen :9999
2. Encrypt (Enigma?XXTEA?CFB)   
3. Hash: TigerHash              
4. Build CFTP message           
5. Connect ???????????????????  2. Accept
6. Send CFTP ?????????????????  3. Read
                                4. Verify hash ?
7. Receive ACK ???????????????  5. Decrypt (CFB?XXTEA?Enigma)
                                6. Save to Received/
8. Success! ?                  7. Send ACK
```

---

## Kompatibilnost

### Kompatibilne Aplikacije

CryptoFileExchange radi sa aplikacijama koje koriste:
- ? Enigma (26-letter alphabet)
- ? XXTEA (16-byte key, zero-padding)
- ? CFB Mode (IV support)
- ? TigerHash (192-bit)

### Test Kompatibilnosti

```bash
dotnet run

# Potraži u output-u:
# "=== DRUGAAPLIKACIJA COMPATIBILITY TEST ==="
```

### Klju?evi za Kompatibilnost

| Sistem | Enigma | XXTEA | CFB | IV |
|--------|--------|-------|-----|-----|
| **CryptoFileExchange** | `MyEnigmaSecretKey2024` | `XXTEAKey12345678` | `CFBModeKey987654` | `""` |
| **Druga aplikacija** | `MyEnigmaSecretKey2024` | `XXTEAKey12345678` | `CFBModeKey987654` | `""` |

---

## Testiranje

### Unit & Integration Tests

```bash
dotnet run

# Output:
# ????????????????????????????????????????
# EnigmaEngine Test Suite
# XXTEAEngine Test Suite
# CFBMode Test Suite
# TigerHash Test Suite
# ...
# DrugaAplikacija Compatibility Test Suite
# ????????????????????????????????????????
# OVERALL TEST SUMMARY
#   Total Test Suites:  10
#   Passed:             XX
#   Failed:             0
# ????????????????????????????????????????
```

### Manual P2P Test

1. Pokreni 2 instance
2. Instance 1: **Start Server** (port 9999)
3. Instance 2: **Send File** ? localhost:9999
4. Proveri `Received/` folder

---

## Troubleshooting

### ? "Hash verification: FAILED"

**Problem:** Klju?evi nisu identi?ni.

**Rešenje:**
1. Proveri Encryption Keys na OBA ra?unara
2. SVA 4 klju?a moraju biti IDENTI?NA
3. Klikni "Apply Keys" na OBA
4. Pokušaj ponovo

### ? "Dekriptovani tekst nije validan Base64 format"

**Problem:** XXTEA padding ili Enigma encoding.

**Rešenje:**
1. Update na najnoviju verziju
2. Proveri klju?eve
3. Pokreni DrugaAplikacijaCompatibilityTest

### ? Server se ne pokre?e

**Problem:** Port ve? u upotrebi.

**Rešenje:**
```powershell
# Prona?i proces
netstat -ano | findstr :9999

# Zatvori ili promeni port
```

### ? "Connection refused"

**Problem:** Firewall ili pogrešna IP.

**Rešenje:**
1. Windows Firewall ? Allow CryptoFileExchange.exe
2. Proveri IP: `ipconfig` (na receiver)
3. Test: `ping 192.168.1.100`

---

## FAQ

### ? Mogu li da menjam klju?eve za svaki fajl?

**Da!** Klikni "Apply Keys" pre svakog transfera.

### ? Da li klju?evi ostaju sa?uvani?

**Ne.** Resetuju se nakon restarta aplikacije.

### ? FSW vs File Exchange klju?evi?

FSW koristi hard-coded klju?eve. File Exchange koristi UI klju?eve.

### ? Kako da šaljem preko interneta?

**Port Forwarding:**
1. Router settings (192.168.1.1)
2. Forward port 9999 ? receiver IP
3. Sender koristi JAVNU IP adresu router-a

---

## ?? Licenca

MIT License

---

## ????? Autor

**Dušan Veli?kovi?**
- GitHub: [@dusanvelickovic](https://github.com/dusanvelickovic)
- Repository: [CryptoFileExchange](https://github.com/dusanvelickovic/CryptoFileExchange)

---

**Verzija:** 2.0.0  
**Poslednje ažuriranje:** 2024-01-15  
**Branch:** dev
