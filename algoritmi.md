# Dokumentacija Algoritama

## Hash Algoritmi

### Tiger Hash

**Lokacija:** `Algorithms/Hash/TigerHash.cs`

Tiger Hash je kriptografski heš algoritam dizajniran da bude brz na 64-bitnim procesorima. Ovaj algoritam generiše heš vrednost od 192 bita (24 bajta).

**Kako radi:**

1. **Inicijalizacija:** Algoritam koristi tri 64-bitne promenljive (a, b, c) sa predefinisanim vrednostima
2. **Padding poruke:** Podaci se dopunjuju (padding) do veličine koja je deljiva sa 64 bajta (veličina bloka)
3. **Procesiranje blokova:** Svaki blok od 64 bajta se konvertuje u 8 blokova po 64 bita (ulong vrednosti)
4. **Tri prolaza (Pass):** 
   - Svaki prolaz koristi različit multiplikator (5, 7, 9)
   - Između prolaza se izvršava KeySchedule funkcija koja menja blokove podataka
5. **Funkcija Pass:**
   - Prolazi kroz svih 8 blokova podataka
   - Koristi 4 S-box tabele (T1, T2, T3, T4) za nelinearne transformacije
   - Izvodi XOR, sabiranje, oduzimanje i množenje operacije
   - Rotira promenljive a, b, c
6. **Kombinovanje rezultata:** Finalne vrednosti se kombinuju sa početnim vrednostima koristeći XOR, sabiranje i oduzimanje
7. **Generisanje heša:** Tri 64-bitne vrednosti (a, b, c) se konvertuju u bajtove i formiraju konačan heš od 24 bajta

**Karakteristike:**
- Veličina heša: 192 bita (24 bajta)
- Veličina bloka: 512 bita (64 bajta)
- Brz na 64-bitnim sistemima
- Koristi lookup tabele za dodatnu sigurnost

---

## Simetrični Algoritmi

### XXTEA (Corrected Block TEA)

**Lokacija:** `Algorithms/Symmetric/XXTEAEngine.cs`

XXTEA je blokovni šifrarnik koji predstavlja poboljšanu verziju algoritma TEA (Tiny Encryption Algorithm). Dizajniran je da bude jednostavan, brz i siguran.

**Kako radi:**

1. **Priprema podataka:**
   - Dodaje se padding tako da dužina podataka bude deljiva sa 4
   - Podaci se konvertuju u niz 32-bitnih unsigned integera (uint)
   - Ključ od 16 bajtova se deli u 4 bloka po 32 bita

2. **Enkripcija (EncryptBlocks):**
   - Broj rundi se računa kao: `q = 6 + 52/n` (gde je n broj blokova)
   - Koristi se delta konstanta: `0x9E3779B9` (bazirana na zlatnom preseku)
   - Suma se povećava za delta u svakoj rundi
   - Svaki blok zavisi od prethodnog i sledećeg bloka (lanac)
   - Koristi MX funkciju za permutacije i substitucije

3. **MX Funkcija:**
   - Kombinuje shift operacije (pomeranje bitova)
   - XOR operacije između susednih blokova
   - Mešanje sa sumom i ključem
   - Formula: `((z>>5 XOR y<<2) + (y>>3 XOR z<<4)) XOR ((sum XOR y) + (key XOR z))`

4. **Dekripcija (DecryptBlocks):**
   - Ista logika kao enkripcija ali u obrnutom redosledu
   - Suma se računa kao `q * DELTA` i smanjuje se u svakoj rundi
   - Prolazi kroz blokove unazad (od kraja ka početku)

5. **Uklanjanje paddinga:** Nakon dekripcije, uklanjaju se dodate nule

**Karakteristike:**
- Veličina ključa: 128 bita (16 bajtova)
- Veličina bloka: varijabilna (minimum 8 bajtova)
- Operacije: shift, XOR, sabiranje, oduzimanje
- Svaki blok zavisi od svih drugih blokova

---

### Enigma Engine

**Lokacija:** `Algorithms/Symmetric/EnigmaEngine.cs`

Enigma je simulacija poznate istorijske šifarke mašine korišćene tokom Drugog svetskog rata. Ova implementacija je prilagođena za rad sa binarnim podacima.

**Kako radi:**

1. **Komponente:**
   - **3 Rotora:** Svaki rotor ima svoju permutaciju slova (26-slovnu supstituciju)
   - **Reflektor:** Obavlja zamenu slova tako da enkripcija i dekripcija budu isti proces
   - **Plugboard:** Opciona razmena parova slova pre ulaska u rotore
   - **Ring Settings:** Podešavanja prstena koja pomeraju internal wiring rotora

2. **Proces enkripcije binarnih podataka:**
   - Podaci se prvo konvertuju u Base64 string (da bi se svi bajtovi mogli predstaviti ASCII karakterima)
   - Base64 string se enkriptuje koristeći Enigma mehanizam
   - Rezultat se vraća kao UTF-8 bajtovi

3. **Rotor mehanika:**
   - Svaki karakter prolazi kroz 3 rotora, reflektor, i nazad kroz rotore
   - Nakon svakog karaktera, rotori se rotiraju (stepping mechanism)
   - Prvi rotor se rotira posle svakog karaktera
   - Drugi rotor se rotira kada prvi završi pun krug
   - Treći rotor se rotira kada drugi završi pun krug

4. **Key Schedule:**
   - Ključ se koristi za inicijalizaciju pozicija rotora
   - Postavlja početne pozicije i ring settings na osnovu ključa

5. **Case-Sensitive mode:**
   - Originalna Enigma radi samo sa A-Z slovima
   - Ova implementacija čuva karaktere koji nisu slova (brojevi, simboli) bez promene
   - Base64 enkodirani tekst omogućava rad sa svim bajtovima

6. **Proces dekripcije:**
   - UTF-8 bajtovi se konvertuju u string
   - String se dekriptuje kroz Enigma (isti proces kao enkripcija zbog reflektora)
   - Dekriptovani tekst se parsira kao Base64 i vraća originalne bajtove

**Karakteristike:**
- Simetrična šifra (enkripcija i dekripcija su isti proces)
- Rotori se kreću posle svakog karaktera
- Svaki karakter se šifruje različito zavisno od pozicije rotora
- Istorijska implementacija prilagođena za moderne podatke

---

## Block Cipher Mode

### CFB Mode (Cipher Feedback)

**Lokacija:** `Algorithms/BlockCipher/CFBMode.cs`

CFB je način rada blokovnih šifri (block cipher mode) koji pretvara blokovnu šifru u stream šifru. Koristi XXTEA algoritam kao osnovnu blokovnu šifru.

**Kako radi:**

1. **Inicijalizacija:**
   - Generiše se ili se koristi postojeći Initialization Vector (IV) od 16 bajtova
   - IV mora biti jedinstven za svaku enkripciju sa istim ključem
   - Ključ se konvertuje u 16-bajtni niz (padding ili truncation)

2. **Enkripcija (CFB Mode):**
   ```
   Prva iteracija:
   - IV se enkriptuje koristeći XXTEA
   - Rezultat se XOR-uje sa prvim blokom plaintexta
   - To postaje prvi blok ciphertexta
   
   Naredne iteracije:
   - Prethodni ciphertext blok se enkriptuje koristeći XXTEA
   - Rezultat se XOR-uje sa sledećim blokom plaintexta
   - To postaje sledeći blok ciphertexta
   ```

3. **Lanac zavisnosti:**
   - Svaki blok ciphertexta zavisi od svih prethodnih blokova plaintexta
   - Promena u jednom bitu plaintexta utiče na sve sledeće blokove ciphertexta

4. **Dekripcija (CFB Mode):**
   ```
   Prva iteracija:
   - IV se enkriptuje koristeći XXTEA (ne dekriptuje!)
   - Rezultat se XOR-uje sa prvim blokom ciphertexta
   - To daje prvi blok plaintexta
   
   Naredne iteracije:
   - Prethodni ciphertext blok se enkriptuje koristeći XXTEA
   - Rezultat se XOR-uje sa sledećim blokom ciphertexta
   - To daje sledeći blok plaintexta
   ```

5. **Handling IV:**
   - Ako IV nije eksplicitno prosleđen, generiše se random IV i dodaje na početak enkriptovanih podataka
   - Pri dekripciji, IV se izvlači iz prvih 16 bajtova
   - Ako je IV eksplicitno prosleđen, ne dodaje se u rezultat (korisnik mora da ga čuva zasebno)

6. **XOR Operacija:**
   - Ključna operacija u CFB modu je XOR
   - XOR je reverzibilna operacija: `(A XOR B) XOR B = A`
   - To omogućava da se dekripcija izvršava na isti način

**Karakteristike:**
- Pretvara blokovnu šifru (XXTEA) u stream šifru
- Ne zahteva padding podataka
- Greška u jednom bitu ciphertexta utiče samo na dva bloka plaintexta pri dekripciji
- IV mora biti jedinstven i može biti javan (ali ne sme biti predvidiv)
- Podržava enkripciju u realnom vremenu (ne mora se čekati ceo plaintext)

**Prednosti CFB moda:**
- Samosinhronizirajući (self-synchronizing) - greške u prenosu ne propagiraju beskonačno
- Može se koristiti za enkripciju podataka čija dužina nije deljiva sa veličinom bloka
- Dekripcija može biti paralelizovana

**Napomena o bezbednosti:**
- Nikada ne koristiti isti IV sa istim ključem više puta
- IV može biti javan, ali mora biti nepredvidiv (random)
- CFB mode pruža povjerljivost ali ne i autentičnost (potrebno je dodati MAC ili koristiti AEAD mode)

---

## Zaključak

Ovaj projekat kombinuje različite kriptografske algoritme:
- **Tiger Hash** za heširanjе podataka (integritet)
- **XXTEA** kao brz i jednostavan blokovni šifrarnik
- **Enigma** kao istorijski značajna simetrična šifra
- **CFB Mode** kao način rada koji omogućava stream enkripciju preko blokovne šifre

Svaki algoritam ima svoju ulogu i karakteristike koje ga čine pogodnim za različite primene u CryptoFileExchange aplikaciji.