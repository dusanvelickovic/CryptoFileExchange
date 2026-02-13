using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoFileExchange.Algorithms.Symmetric
{
    /// <summary>
    /// Enigma šifarski algoritam kompatibilan sa DrugaAplikacija
    /// Radi sa 26-slovnom azbukom (A-Z), preskače sve druge karaktere
    /// 
    /// Komponente:
    /// - 3 Rotora: svaki sa 26-slovnom supstitucijom
    /// - Reflektor: zamena slova - enkripcija i dekripcija su isti proces
    /// - Plugboard: opciona razmena parova slova
    /// - Ring Settings: pomeranje internal wiring rotora
    /// </summary>
    internal class EnigmaEngine
    {
        // Predefinisane supstitucione tabele za tri rotora
        private const string DEFAULT_ROTOR1 = "EKMFLGDQVZNTOWYHXUSPAIBRCJ";
        private const string DEFAULT_ROTOR2 = "AJDKSIRUXBLHWTMCQGZNPYFVOE";
        private const string DEFAULT_ROTOR3 = "BDFHJLCPRTXVZNYEIWGAKMUSQO";
        
        // Reflektor: zamena slova (omogućava simetričnu enkripciju/dekripciju)
        private const string REFLECTOR = "YRUHQSLDPXNGOKMIEBFZCWVJAT";

        private string _rotor1;
        private string _rotor2;
        private string _rotor3;
        private string _reflector;

        // Trenutne pozicije rotora (0-25, rotiraju se tokom enkripcije)
        private int _position1 = 0;
        private int _position2 = 0;
        private int _position3 = 0;

        // Ring settings: pomeranje internal wiring rotora (0-25)
        private int _ring1 = 0;
        private int _ring2 = 0;
        private int _ring3 = 0;

        // Plugboard: opciona razmena parova slova pre i posle rotora
        private Dictionary<char, char> _plugboard;

        public EnigmaEngine()
        {
            _rotor1 = DEFAULT_ROTOR1;
            _rotor2 = DEFAULT_ROTOR2;
            _rotor3 = DEFAULT_ROTOR3;
            _reflector = REFLECTOR;
            _plugboard = new Dictionary<char, char>();
            ResetPositions();
            ResetRingSettings();
        }

        /// <summary>
        /// Enkripcija niza bajtova
        /// Proces: binarni podaci → Base64 string → Enigma enkripcija → UTF-8 bajtovi
        /// </summary>
        public byte[] Encrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            // Konverzija binarnih podataka u Base64 string (Case-Sensitive mode)
            string base64Text = Convert.ToBase64String(data);
            
            // Enkripcija Base64 stringa koristeći Enigma mehanizam
            string encryptedText = EncryptString(base64Text, key, caseSensitive: true);
            
            // Vraćanje kao UTF-8 bajtovi
            return Encoding.UTF8.GetBytes(encryptedText);
        }

        /// <summary>
        /// Dekripcija niza bajtova
        /// Proces: UTF-8 bajtovi → string → Enigma dekripcija → parsiranje Base64 → originalni bajtovi
        /// </summary>
        public byte[] Decrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            // Uklanjanje trailing null bajtova iz XXTEA paddinga
            int dataLength = data.Length;
            while (dataLength > 0 && data[dataLength - 1] == 0)
            {
                dataLength--;
            }

            byte[] trimmedData = new byte[dataLength];
            Array.Copy(data, trimmedData, dataLength);

            // Konverzija UTF-8 bajtova u string
            string encryptedText = Encoding.UTF8.GetString(trimmedData);
            
            // Dekripcija stringa kroz Enigma (simetričan proces kao enkripcija)
            string decryptedText = DecryptString(encryptedText, key, caseSensitive: true);
            
            // Uklanjanje trailing null karaktera iz dekriptovanog stringa
            decryptedText = decryptedText.TrimEnd('\0');
            
            // Parsiranje Base64 i vraćanje originalnih bajtova
            try
            {
                return Convert.FromBase64String(decryptedText);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Dekriptovani tekst nije validan Base64 format. Proverite kljuc ili integritet fajla.", ex);
            }
        }

        /// <summary>
        /// Glavna enkripciona logika: prolazak karaktera kroz rotore, reflektor, i nazad
        /// </summary>
        private string EncryptString(string plaintext, string key, bool caseSensitive = false)
        {
            // Resetovanje pozicija rotora i postavljanje početnih pozicija iz ključa
            ResetPositions();
            SetRotorPositions(key); // Key Schedule: inicijalizacija pozicija rotora

            StringBuilder result = new StringBuilder();
            string text = caseSensitive ? plaintext : plaintext.ToUpper();

            foreach (char c in text)
            {
                // Samo A-Z karakteri se enkriptuju, svi drugi se čuvaju (Case-Sensitive mode)
                if (!(c >= 'A' && c <= 'Z'))
                {
                    result.Append(c);
                    continue;
                }

                // Stepping mechanism: rotiranje rotora nakon svakog karaktera
                RotateRotors();

                char ch = c;

                // 1. Plugboard: opciona razmena slova pre ulaska u rotore
                if (_plugboard.ContainsKey(ch))
                {
                    ch = _plugboard[ch];
                }

                // 2. Prolazak napred kroz tri rotora (desno ka levo)
                ch = RotorForward(_rotor3, ch, _position3, _ring3);
                ch = RotorForward(_rotor2, ch, _position2, _ring2);
                ch = RotorForward(_rotor1, ch, _position1, _ring1);

                // 3. Reflektor: zamena slova (omogućava simetričnost)
                ch = _reflector[ch - 'A'];

                // 4. Prolazak nazad kroz tri rotora (levo ka desno)
                ch = RotorBackward(_rotor1, ch, _position1, _ring1);
                ch = RotorBackward(_rotor2, ch, _position2, _ring2);
                ch = RotorBackward(_rotor3, ch, _position3, _ring3);

                // 5. Plugboard: opciona razmena slova nakon izlaska iz rotora
                if (_plugboard.ContainsKey(ch))
                {
                    ch = _plugboard[ch];
                }

                result.Append(ch);
            }

            return result.ToString();
        }

        /// <summary>
        /// Dekripcija stringa (Enigma je simetričan, dekripcija = enkripcija)
        /// </summary>
        private string DecryptString(string ciphertext, string key, bool caseSensitive = false)
        {
            // Enigma je simetričan algoritam: dekripcija je isti proces kao enkripcija
            return EncryptString(ciphertext, key, caseSensitive);
        }

        // Prolazak karaktera kroz rotor napred (ulaz → izlaz)
        private char RotorForward(string rotor, char input, int position, int ring)
        {
            // Kalkulacija offseta sa pozicijom i ring settingom
            int offset = (input - 'A' + position - ring + 26) % 26;
            char output = rotor[offset];
            int result = (output - 'A' - position + ring + 26) % 26;
            return (char)('A' + result);
        }

        // Prolazak karaktera kroz rotor unazad (izlaz → ulaz)
        private char RotorBackward(string rotor, char input, int position, int ring)
        {
            // Kalkulacija offseta i pronalaženje inverza supstitucije
            int offset = (input - 'A' + position - ring + 26) % 26;
            int index = rotor.IndexOf((char)('A' + offset));
            int result = (index - position + ring + 26) % 26;
            return (char)('A' + result);
        }

        // Stepping mechanism: rotiranje rotora nakon svakog karaktera
        private void RotateRotors()
        {
            // Prvi rotor se rotira posle svakog karaktera
            _position1 = (_position1 + 1) % 26;
            
            // Drugi rotor rotiranje (kada prvi pređe pun krug)
            if (_position1 == 0)
            {
                _position2 = (_position2 + 1) % 26;
                
                // Treći rotor rotiranje (kada drugi pređe pun krug)
                if (_position2 == 0)
                    _position3 = (_position3 + 1) % 26;
            }
        }

        // Key Schedule: inicijalizacija pozicija rotora iz ključa (početne pozicije)
        private void SetRotorPositions(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 3)
            {
                _position1 = 0;
                _position2 = 0;
                _position3 = 0;
                return;
            }

            // Konverzija prvih 3 karaktera ključa u pozicije rotora (A=0, B=1, ..., Z=25)
            key = key.ToUpper();
            _position1 = (key[0] - 'A' + 26) % 26;
            _position2 = (key.Length > 1 ? (key[1] - 'A' + 26) % 26 : 0);
            _position3 = (key.Length > 2 ? (key[2] - 'A' + 26) % 26 : 0);
        }

        private void ResetPositions()
        {
            _position1 = 0;
            _position2 = 0;
            _position3 = 0;
        }

        private void ResetRingSettings()
        {
            _ring1 = 0;
            _ring2 = 0;
            _ring3 = 0;
        }
    }
}

