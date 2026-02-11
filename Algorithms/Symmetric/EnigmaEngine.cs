using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoFileExchange.Algorithms.Symmetric
{
    /// <summary>
    /// Enigma cipher implementation compatible with DrugaAplikacija
    /// Works with 26-letter alphabet (A-Z), skips all other characters
    /// </summary>
    internal class EnigmaEngine
    {
        private const string DEFAULT_ROTOR1 = "EKMFLGDQVZNTOWYHXUSPAIBRCJ";
        private const string DEFAULT_ROTOR2 = "AJDKSIRUXBLHWTMCQGZNPYFVOE";
        private const string DEFAULT_ROTOR3 = "BDFHJLCPRTXVZNYEIWGAKMUSQO";
        private const string REFLECTOR = "YRUHQSLDPXNGOKMIEBFZCWVJAT";

        private string _rotor1;
        private string _rotor2;
        private string _rotor3;
        private string _reflector;

        private int _position1 = 0;
        private int _position2 = 0;
        private int _position3 = 0;

        private int _ring1 = 0;
        private int _ring2 = 0;
        private int _ring3 = 0;

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
        /// Encrypt byte array (converts to Base64 first, encrypts, returns UTF-8 bytes)
        /// </summary>
        public byte[] Encrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            // Convert to Base64 string first
            string base64Text = Convert.ToBase64String(data);
            
            // Encrypt the Base64 string with case-sensitive mode
            string encryptedText = EncryptString(base64Text, key, caseSensitive: true);
            
            // Return as UTF-8 encoded bytes
            return Encoding.UTF8.GetBytes(encryptedText);
        }

        /// <summary>
        /// Decrypt byte array (converts from UTF-8, decrypts, converts from Base64)
        /// </summary>
        public byte[] Decrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            // Trim trailing null bytes from XXTEA padding
            int dataLength = data.Length;
            while (dataLength > 0 && data[dataLength - 1] == 0)
            {
                dataLength--;
            }

            byte[] trimmedData = new byte[dataLength];
            Array.Copy(data, trimmedData, dataLength);

            // Convert UTF-8 bytes to string
            string encryptedText = Encoding.UTF8.GetString(trimmedData);
            
            // Decrypt the string with case-sensitive mode
            string decryptedText = DecryptString(encryptedText, key, caseSensitive: true);
            
            // Trim any trailing null characters from decrypted string (from Enigma preserving non-A-Z chars)
            decryptedText = decryptedText.TrimEnd('\0');
            
            // Convert from Base64 to original bytes
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
        /// Encrypt string (main encryption logic)
        /// </summary>
        private string EncryptString(string plaintext, string key, bool caseSensitive = false)
        {
            ResetPositions();
            SetRotorPositions(key);

            StringBuilder result = new StringBuilder();
            string text = caseSensitive ? plaintext : plaintext.ToUpper();

            foreach (char c in text)
            {
                // Only encrypt A-Z characters, skip all others
                if (!(c >= 'A' && c <= 'Z'))
                {
                    result.Append(c);
                    continue;
                }

                RotateRotors();

                char ch = c;

                // 1. Plugboard (before entering rotors)
                if (_plugboard.ContainsKey(ch))
                {
                    ch = _plugboard[ch];
                }

                // 2. Forward through rotors
                ch = RotorForward(_rotor3, ch, _position3, _ring3);
                ch = RotorForward(_rotor2, ch, _position2, _ring2);
                ch = RotorForward(_rotor1, ch, _position1, _ring1);

                // 3. Reflector
                ch = _reflector[ch - 'A'];

                // 4. Backward through rotors
                ch = RotorBackward(_rotor1, ch, _position1, _ring1);
                ch = RotorBackward(_rotor2, ch, _position2, _ring2);
                ch = RotorBackward(_rotor3, ch, _position3, _ring3);

                // 5. Plugboard (after exiting rotors)
                if (_plugboard.ContainsKey(ch))
                {
                    ch = _plugboard[ch];
                }

                result.Append(ch);
            }

            return result.ToString();
        }

        /// <summary>
        /// Decrypt string (Enigma is symmetric, so same as encrypt)
        /// </summary>
        private string DecryptString(string ciphertext, string key, bool caseSensitive = false)
        {
            return EncryptString(ciphertext, key, caseSensitive);
        }

        private char RotorForward(string rotor, char input, int position, int ring)
        {
            int offset = (input - 'A' + position - ring + 26) % 26;
            char output = rotor[offset];
            int result = (output - 'A' - position + ring + 26) % 26;
            return (char)('A' + result);
        }

        private char RotorBackward(string rotor, char input, int position, int ring)
        {
            int offset = (input - 'A' + position - ring + 26) % 26;
            int index = rotor.IndexOf((char)('A' + offset));
            int result = (index - position + ring + 26) % 26;
            return (char)('A' + result);
        }

        private void RotateRotors()
        {
            _position1 = (_position1 + 1) % 26;
            if (_position1 == 0)
            {
                _position2 = (_position2 + 1) % 26;
                if (_position2 == 0)
                    _position3 = (_position3 + 1) % 26;
            }
        }

        private void SetRotorPositions(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 3)
            {
                _position1 = 0;
                _position2 = 0;
                _position3 = 0;
                return;
            }

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

