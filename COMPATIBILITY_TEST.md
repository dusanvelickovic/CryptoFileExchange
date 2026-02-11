# Test kompatibilnosti sa DrugaAplikacija

## Problem
I dalje dobijate grešku "Dekriptovani tekst nije validan Base64 format" kada šaljete fajl iz CryptoFileExchange ka DrugaAplikacija serveru.

## Provera klju?eva

### Vaši klju?evi (CryptoFileExchange)
```
DEFAULT_ENIGMA_KEY = "MyEnigmaSecretKey2024"
DEFAULT_XXTEA_KEY = "XXTEAKey12345678"
DEFAULT_CFB_KEY = "CFBModeKey987654"
DEFAULT_CFB_IV = "InitVector000000"
```

### DrugaAplikacija klju?evi
DrugaAplikacija TCPServer prima klju?eve kroz konstruktor:
```csharp
public TCPServer(int port, string receiveDirectory, 
                 string enigmaKey, string xxteaKey, 
                 string cfbKey, string cfbIV)
```

**PROBLEM:** Ne znamo koji klju?evi se koriste u DrugaAplikacija!

## Kako proveriti

### Opcija 1: Provera UI koda DrugaAplikacija
Prona?ite fajl koji kreira TCPServer (verovatno Form1.cs ili sli?no) i vidite koje klju?eve koristi.

### Opcija 2: Koristite ISTE testne klju?eve
U DrugaAplikacija aplikaciji, kada kreirate TCPServer, koristite:
```csharp
var server = new TCPServer(
    port: 9999,
    receiveDirectory: @"C:\Temp\Received",
    enigmaKey: "MyEnigmaSecretKey2024",
    xxteaKey: "XXTEAKey12345678",
    cfbKey: "CFBModeKey987654",
    cfbIV: "InitVector000000"
);
```

### Opcija 3: Debug test
Kreirajte mali test program:

```csharp
// Test: Enkriptuj u CryptoFileExchange, dekriptuj u DrugaAplikacija
byte[] originalData = Encoding.UTF8.GetBytes("TEST DATA");

// CryptoFileExchange - Encrypt
var enigma1 = new EnigmaEngine();
byte[] step1 = enigma1.Encrypt(originalData, "MyEnigmaSecretKey2024");

var xxtea1 = new XXTEAEngine();
byte[] xxteaKey = StringToKeyBytes("XXTEAKey12345678", 16);
byte[] step2 = xxtea1.Encrypt(step1, xxteaKey);

var cfb1 = new CFBMode();
byte[] cfbIV = StringToKeyBytes("InitVector000000", 16);
byte[] encrypted = cfb1.Encrypt(step2, "CFBModeKey987654", cfbIV);

// DrugaAplikacija - Decrypt (reverse order)
var cfb2 = new XXTEACFB();
byte[] cfbKeyBytes = StringToKeyBytes("CFBModeKey987654", 16);
byte[] cfbIVBytes = StringToKeyBytes("InitVector000000", 16);
byte[] decStep1 = cfb2.Decrypt(encrypted, cfbKeyBytes, cfbIVBytes);

var xxtea2 = new XXTEA();
byte[] xxteaKeyBytes = StringToKeyBytes("XXTEAKey12345678", 16);
byte[] decStep2 = xxtea2.Decrypt(decStep1, xxteaKeyBytes);

var enigma2 = new EnigmaCipher();
byte[] decrypted = enigma2.DecryptFile(decStep2, "MyEnigmaSecretKey2024");

// Provera
string result = Encoding.UTF8.GetString(decrypted);
Console.WriteLine($"Original: TEST DATA");
Console.WriteLine($"Decrypted: {result}");
Console.WriteLine($"Match: {result == "TEST DATA"}");
```

## Naj?eš?i problemi

### 1. RAZLI?ITI KLJU?EVI ?
```
CryptoFileExchange: "MyEnigmaSecretKey2024"
DrugaAplikacija:    "DifferentKey123"
? Greška: "Dekriptovani tekst nije validan Base64 format"
```

### 2. RAZLI?IT REDOSLED ALGORITAMA ?
```
CryptoFileExchange metadata.EncryptionAlgorithm = "Enigma -> XXTEA -> CFB"
DrugaAplikacija o?ekuje:                         "Enigma -> XXTEA -> CFB"
```
Ovo bi trebalo da bude OK jer vaša aplikacija šalje "Enigma -> XXTEA -> CFB".

### 3. RAZLI?ITI IV VREDNOSTI ?
```
CryptoFileExchange: "InitVector000000"
DrugaAplikacija:    "DifferentIV12345"
? CFB dekriptovanje ne?e raditi
```

## Šta provjeriti SADA

1. **Otvorite kod DrugaAplikacija aplikacije** (verovatno Form1.cs ili Main program)
2. **Prona?ite gde se kreira TCPServer**
3. **Proverite koje klju?eve koristi**
4. **Uporedite sa klju?evima u FileExchangePanel.cs (linije 23-26)**

## Ako klju?evi ne odgovaraju

**Ažurirajte FileExchangePanel.cs:**
```csharp
private const string DEFAULT_ENIGMA_KEY = "ISTIkaoUDrugojAplikaciji";
private const string DEFAULT_XXTEA_KEY = "ISTIkaoUDrugojAplikaciji";
private const string DEFAULT_CFB_KEY = "ISTIkaoUDrugojAplikaciji";
private const string DEFAULT_CFB_IV = "ISTIkaoUDrugojAplikaciji";
```

**ILI ažurirajte DrugaAplikacija da koristi vaše klju?eve:**
```csharp
enigmaKey: "MyEnigmaSecretKey2024"
xxteaKey: "XXTEAKey12345678"
cfbKey: "CFBModeKey987654"
cfbIV: "InitVector000000"
```
