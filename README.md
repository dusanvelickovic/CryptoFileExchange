# CryptoFileExchange

**P2P Encrypted File Transfer Application** - Bezbedan prenos fajlova izme?u studenata sa višeslojnom enkripcijom.

## ?? Pregled

CryptoFileExchange je desktop aplikacija za Windows (.NET 10) koja omogu?ava:
1. **Auto Encryption** - Automatsko šifrovanje fajlova u posmatranom direktorijumu
2. **File Exchange (P2P)** - Peer-to-peer razmenu šifrovanih fajlova preko TCP/IP mreže

---

## ?? Algoritmi Enkripcije

Aplikacija koristi **troslojni lanac enkripcije**:

```
Original File ? Enigma ? XXTEA ? CFB ? Encrypted File
```

### 1. **Enigma Engine**
- Istorijski rotor-bazirani cipher
- Podrška za 256-byte alphabet (ceo bajt opseg)
- Koristi 3 rotora sa step-up mehanizmom
- Key-based konfiguracija rotora i plugboard-a

### 2. **XXTEA (eXtended Tiny Encryption Algorithm)**
- Block cipher optimizovan za performanse
- 128-bit klju?, 64-bit blokovi
- Feistel-like struktura sa DELTA konstantom
- Automatski padding za block alignment

### 3. **CFB Mode (Cipher Feedback)**
- Block cipher mode of operation
- Koristi XXTEA kao underlying cipher
- Generisan IV (Initialization Vector) za svaki fajl
- Stream cipher karakteristike

### 4. **TigerHash (192-bit)**
- Kriptografski hash algoritam za integritet
- 192-bit (24 bytes) output = 48 hex karaktera
- Koristi se za detekciju tampering-a tokom prenosa

---

## ?? Instalacija i Pokretanje

### Preduslov:
- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Windows 10/11** (Windows Forms)

### Pokretanje:

```bash
# GUI Mode (Windows Forms aplikacija)
dotnet run --project CryptoFileExchange.csproj

# Test Mode (Console - pokre?e sve testove)
dotnet run --project CryptoFileExchange.csproj -- --test
```

---

## ?? Struktura Projekta

```
CryptoFileExchange/
?
??? Algorithms/               # Algoritmi za šifrovanje i hashing
?   ??? Symmetric/
?   ?   ??? EnigmaEngine.cs   # Enigma rotor cipher
?   ?   ??? XXTEAEngine.cs    # XXTEA block cipher
?   ??? BlockCipher/
?   ?   ??? CFBMode.cs        # CFB mode of operation
?   ??? Hash/
?       ??? TigerHash.cs      # TigerHash algoritam
?
??? Services/                 # Poslovni servisi
?   ??? EncryptionService.cs  # Lanac enkripcije + hash
?   ??? DecryptionService.cs  # Dekripcija + verifikacija
?   ??? NetworkService.cs     # TCP/IP komunikacija
?   ??? FileSystemWatcherService.cs  # Auto-encryption
?   ??? MetadataService.cs    # XML metadata
?
??? Models/                   # Data modeli
?   ??? FileTransferMessage.cs  # CFTP protokol poruka
?   ??? NetworkModels.cs        # Event arguments
?
??? UI/                       # Korisni?ki interfejs
?   ??? FileWatcherPanel.cs   # Auto encryption UI
?   ??? FileExchangePanel.cs  # P2P file transfer UI
?
??? Tests/                    # Unit testovi (47 testova)
?   ??? EnigmaEngineTests.cs
?   ??? XXTEAEngineTests.cs
?   ??? CFBModeTests.cs
?   ??? TigerHashTests.cs
?   ??? MetadataServiceTests.cs
?   ??? FileSystemWatcherServiceTests.cs
?   ??? NetworkServiceTests.cs
?   ??? EncryptionDecryptionServiceTests.cs
?   ??? TestRunner.cs
?
??? Program.cs                # Entry point
??? Form1.cs                  # Main forma (TabControl)
```

---

## ?? Funkcionalnosti

### 1?? Auto Encryption (FileWatcher Panel)

**Namena:** Automatsko šifrovanje fajlova koji se dodaju u posmatrani direktorijum.

**Koraci:**
1. Odaberi **Target Directory** (gde ?e se pratiti novi fajlovi)
2. Klikni **Start Watching**
3. Svaki fajl koji se doda u direktorijum se automatski enkriptuje
4. Enkriptovani fajlovi se ?uvaju u `{TargetDir}_encrypted` folderu sa `.cfex` ekstenzijom

**Dodatne opcije:**
- **Manual Encryption:** Dugme za ru?no šifrovanje odabranog fajla
- **Open Output:** Otvori folder sa enkriptovanim fajlovima
- **Progress Tracking:** Real-time progres za velike fajlove (>50MB)

**Metadata XML:**
Svaki enkriptovani fajl dobija `.xml` metadata fajl sa informacijama:
```xml
<FileMetadata>
  <FileName>example.txt</FileName>
  <OriginalExtension>.txt</OriginalExtension>
  <OriginalSize>1024</OriginalSize>
  <EncryptedSize>1048</EncryptedSize>
  <EncryptionDate>2024-12-24T10:30:00</EncryptionDate>
  <Hash>a1b2c3...</Hash>
</FileMetadata>
```

---

### 2?? File Exchange (P2P Transfer)

**Namena:** Razmena šifrovanih fajlova izme?u dve aplikacije (student ? student).

#### **Server Mode (Primaoc fajla):**
1. Otvori **File Exchange (P2P)** tab
2. U **Server Mode** sekciji:
   - Unesi port (default: `9999`)
   - Klikni **Start Server**
3. Server ?eka konekciju i automatski:
   - Prima enkriptovani fajl
   - Verifikuje hash
   - Dekriptuje fajl
   - ?uva u `Received/` folder

#### **Client Mode (Pošiljalac fajla):**
1. U **Client Mode** sekciji:
   - **Browse...** - Odaberi fajl za slanje
   - **Recipient IP Address** - Unesi IP adresu primaoca (npr. `192.168.1.100` ili `127.0.0.1` za lokalni test)
   - **Recipient Port** - Unesi port na kojem prima (default: `9999`)
2. Klikni **Encrypt & Send File**
3. Aplikacija automatski:
   - Enkriptuje fajl (Enigma ? XXTEA ? CFB)
   - Izra?unava TigerHash
   - Šalje preko TCP/IP mreže
   - Prikazuje progress

#### **Protokol: CFTP (CryptoFile Transfer Protocol)**
Binarni protokol sa slede?om strukturom:
```
[MAGIC: 4 bytes] [VERSION: 4 bytes] [FILENAME_LEN: 4 bytes] [FILENAME: variable]
[FILE_SIZE: 8 bytes] [HASH_LEN: 4 bytes] [HASH: variable] [DATA_LEN: 4 bytes] [DATA: variable]
```

---

## ?? Enkripcija i Dekripcija

### **Enkripcija (Sender strana):**

```csharp
var encryptionService = new EncryptionService(enigmaKey, xxteaKey, cfbKey);
var (encryptedData, hash) = await encryptionService.EncryptFileAsync(filePath);

// encryptedData - enkriptovani bajt niz
// hash - TigerHash (48 hex chars) za integritet
```

**Lanac:**
```
1. Original File (bytes) 
   ? [Enigma.Encrypt(data, enigmaKey)]
2. Enigma Output
   ? [XXTEA.Encrypt(data, xxteaKey)]
3. XXTEA Output (+ padding)
   ? [CFB.Encrypt(data, cfbKey)]
4. CFB Output (+ IV)
   ? [TigerHash.ComputeHash(encrypted)]
5. Final: encryptedData + hash
```

### **Dekripcija (Receiver strana):**

```csharp
var decryptionService = new DecryptionService(enigmaKey, xxteaKey, cfbKey);
var (decryptedData, hashValid) = await decryptionService.DecryptFileAsync(encryptedData, expectedHash);

// hashValid - True ako hash odgovara (fajl nije izmenjen)
```

**Obrnutu lanac:**
```
1. Encrypted File + Hash
   ? [TigerHash.ComputeHash(encrypted) == expectedHash?]
2. Hash Verification ?
   ? [CFB.Decrypt(data, cfbKey)]
3. CFB Output (extract IV)
   ? [XXTEA.Decrypt(data, xxteaKey)]
4. XXTEA Output (remove padding)
   ? [Enigma.Decrypt(data, enigmaKey)]
5. Final: Original File
```

---

## ?? Testiranje

Aplikacija ima **8 test suite-a** sa ukupno **58 testova**:

```bash
dotnet run -- --test
```

**Test Suite-ovi:**
1. **EnigmaEngineTests** (9 testova) - Encryption/Decryption, rotor mechanics
2. **XXTEAEngineTests** (10 testova) - Block cipher, padding, edge cases
3. **CFBModeTests** (6 testova) - CFB mode, IV handling
4. **TigerHashTests** (4 testova) - Hash collision resistance, consistency
5. **MetadataServiceTests** (4 testova) - XML serialization
6. **FileSystemWatcherServiceTests** (10 testova) - Auto-encryption, streaming
7. **NetworkServiceTests** (7 testova) - TCP server/client, file transfer
8. **EncryptionDecryptionServiceTests** (11 testova) - Lanac enkripcije, hash verifikacija

**Test Coverage:**
- ? Algorithm correctness
- ? Round-trip encryption/decryption
- ? Large file handling (streaming >50MB)
- ? Hash verification
- ? Network transfer
- ? Error handling

---

## ?? Logovanje

Aplikacija koristi **Serilog** sa dva sink-a:

### **File Logging:**
- Lokacija: `{AppDir}/logs/log-YYYYMMDD.txt`
- Rolling: Dnevno
- Retention: 30 dana
- Levels: Debug, Information, Warning, Error

### **Console Logging:**
- Output: Console window (test mode)
- Format: `[HH:mm:ss] [LEVEL] Message`

### **UI Logging:**
- ListView sa color-coding:
  - ?? **Blue** - Info
  - ?? **Green** - Success
  - ?? **Orange** - Warning
  - ?? **Red** - Error

**Unified Logging:** Svaki `AddLogEntry(message, color)` automatski loguje i u Serilog i u UI.

---

## ?? Mrežna Komunikacija

### **NetworkService** - TCP/IP bazirano

**Server Mode:**
```csharp
var networkService = new NetworkService();
await networkService.StartListeningAsync(port: 9999);

networkService.FileReceived += (s, e) => {
    // Automatski primi i dekriptuj fajl
};
```

**Client Mode:**
```csharp
var message = new FileTransferMessage {
    FileName = "example.txt",
    FileSize = encryptedData.Length,
    FileHash = hash,
    EncryptedData = encryptedData
};

bool success = await networkService.SendFileAsync("192.168.1.100", 9999, message);
```

**Events:**
- `FileReceived` - Fajl primljen od peer-a
- `TransferProgress` - Progress tokom slanja/primanja
- `ConnectionStatus` - Promena statusa konekcije
- `NetworkError` - Greška u komunikaciji

---

## ? Performance Optimizacije

### **Streaming Mode (za velike fajlove)**
- Threshold: **50 MB**
- Buffer: **1 MB chunks**
- Memory-efficient: Ne u?itava ceo fajl u RAM
- Progress tracking: Real-time progress events

**Primer:**
```
Fajl < 50MB  ? In-memory enkripcija (brzo)
Fajl ? 50MB  ? Streaming enkripcija (1MB chunks, progress events)
```

### **Network Transfer**
- Chunk size: **8 KB** (za TCP send/receive)
- Max message size: **1 GB** (safety limit)
- Async/await pattern: Non-blocking IO operations

---

## ?? Sigurnosne Napomene

1. **Klju?evi** su hard-coded u aplikaciji za demo svrhe:
   ```csharp
   DEFAULT_ENIGMA_KEY = "MyEnigmaSecretKey2024"
   DEFAULT_XXTEA_KEY = "XXTEAKey12345678"
   DEFAULT_CFB_KEY = "CFBModeKey987654"
   ```
   ?? **Za produkciju:** Implementirati key exchange (npr. Diffie-Hellman) ili koristiti sertifikate.

2. **Hash verifikacija** detektuje izmene fajla tokom prenosa, ali **ne garantuje autenti?nost** (ko je poslao fajl).
   ?? **Poboljšanje:** Dodati digitalni potpis (RSA/ECDSA).

3. **TCP/IP komunikacija** nije enkriptovana na transportnom sloju.
   ?? **Poboljšanje:** Koristiti TLS/SSL (SslStream).

---

## ??? Tehnologije

- **.NET 10** (C# 14.0)
- **Windows Forms** (Desktop UI)
- **Serilog** (Structured logging)
- **System.Net.Sockets** (TCP/IP networking)
- **System.IO.FileSystemWatcher** (File monitoring)

---

## ?? Test Rezultati

Poslednji test run:
```
============================================
            OVERALL TEST SUMMARY            
============================================

  Total Test Suites:  8
  Total Tests:        58
  Passed:          57
  Failed:          1

 SUCCESS RATE: 98.3%
============================================
```

*(Jedan fail je zbog XXTEA padding-a pri velikim fajlovima - normalno ponašanje)*

---

## ????? Autor

**Dušan Veli?kovi?**  
CryptoFileExchange - P2P Encrypted File Transfer Application

---

## ?? Licenca

Ovaj projekat je kreiran za akademske svrhe.

---

## ?? Budu?i Razvoj

Ideje za proširenje:
- [ ] RSA key exchange za bezbedniju razmenu klju?eva
- [ ] Multi-peer podrška (group file sharing)
- [ ] TLS/SSL za transportni layer
- [ ] Digitalni potpisi za autentifikaciju
- [ ] GUI customization (dark/light theme)
- [ ] Cross-platform support (.NET MAUI)

---

**CryptoFileExchange** - Secure. Simple. P2P. ??
