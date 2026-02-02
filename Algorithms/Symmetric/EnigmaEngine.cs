using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoFileExchange.Algorithms.Symmetric
{
    internal class EnigmaEngine
    {
        private readonly List<Rotor> _rotors;
        private readonly Reflector _reflector;
        private readonly Plugboard _plugboard;
        private const int ALPHABET_SIZE = 256;

        public EnigmaEngine()
        {
            _rotors = new List<Rotor>();
            _reflector = new Reflector();
            _plugboard = new Plugboard();
        }

        public byte[] Encrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            ConfigureFromKey(key);
            return ProcessData(data);
        }

        public byte[] Decrypt(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            ConfigureFromKey(key);
            return ProcessData(data);
        }

        private void ConfigureFromKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty");

            _rotors.Clear();
            
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            int seed = keyBytes.Sum(b => b);

            Random rnd = new Random(seed);
            
            int rotorCount = 3 + (seed % 3);
            for (int i = 0; i < rotorCount; i++)
            {
                _rotors.Add(new Rotor(i, rnd.Next(ALPHABET_SIZE)));
            }

            _plugboard.Configure(keyBytes);
        }

        private byte[] ProcessData(byte[] data)
        {
            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                byte input = data[i];
                
                input = _plugboard.Transform(input);
                
                foreach (var rotor in _rotors)
                {
                    input = rotor.Forward(input);
                }
                
                input = _reflector.Reflect(input);
                
                for (int j = _rotors.Count - 1; j >= 0; j--)
                {
                    input = _rotors[j].Backward(input);
                }
                
                input = _plugboard.Transform(input);
                
                result[i] = input;
                
                RotateRotors();
            }

            return result;
        }

        private void RotateRotors()
        {
            bool rotate = true;
            foreach (var rotor in _rotors)
            {
                if (rotate)
                {
                    rotate = rotor.Rotate();
                }
                else
                {
                    break;
                }
            }
        }

        private class Rotor
        {
            private readonly byte[] _wiring;
            private readonly byte[] _inverseWiring;
            private int _position;
            private readonly int _notch;

            public Rotor(int rotorId, int initialPosition)
            {
                _wiring = new byte[ALPHABET_SIZE];
                _inverseWiring = new byte[ALPHABET_SIZE];
                _position = initialPosition % ALPHABET_SIZE;
                _notch = (rotorId * 37 + 67) % ALPHABET_SIZE;

                InitializeWiring(rotorId);
            }

            private void InitializeWiring(int rotorId)
            {
                Random rnd = new Random(rotorId * 1000 + 12345);
                List<byte> available = Enumerable.Range(0, ALPHABET_SIZE).Select(x => (byte)x).ToList();

                for (int i = 0; i < ALPHABET_SIZE; i++)
                {
                    int index = rnd.Next(available.Count);
                    _wiring[i] = available[index];
                    _inverseWiring[available[index]] = (byte)i;
                    available.RemoveAt(index);
                }
            }

            public byte Forward(byte input)
            {
                int shifted = (input + _position) % ALPHABET_SIZE;
                int output = _wiring[shifted];
                return (byte)((output - _position + ALPHABET_SIZE) % ALPHABET_SIZE);
            }

            public byte Backward(byte input)
            {
                int shifted = (input + _position) % ALPHABET_SIZE;
                int output = _inverseWiring[shifted];
                return (byte)((output - _position + ALPHABET_SIZE) % ALPHABET_SIZE);
            }

            public bool Rotate()
            {
                _position = (_position + 1) % ALPHABET_SIZE;
                return _position == _notch;
            }
        }

        private class Reflector
        {
            private readonly byte[] _wiring;

            public Reflector()
            {
                _wiring = new byte[ALPHABET_SIZE];
                InitializeWiring();
            }

            private void InitializeWiring()
            {
                Random rnd = new Random(54321);
                List<byte> available = Enumerable.Range(0, ALPHABET_SIZE).Select(x => (byte)x).ToList();

                for (int i = 0; i < ALPHABET_SIZE; i += 2)
                {
                    if (available.Count < 2) break;

                    int index1 = rnd.Next(available.Count);
                    byte val1 = available[index1];
                    available.RemoveAt(index1);

                    int index2 = rnd.Next(available.Count);
                    byte val2 = available[index2];
                    available.RemoveAt(index2);

                    _wiring[val1] = val2;
                    _wiring[val2] = val1;
                }
            }

            public byte Reflect(byte input)
            {
                return _wiring[input];
            }
        }

        private class Plugboard
        {
            private readonly Dictionary<byte, byte> _connections;

            public Plugboard()
            {
                _connections = new Dictionary<byte, byte>();
            }

            public void Configure(byte[] keyBytes)
            {
                _connections.Clear();

                int pairCount = Math.Min(10, keyBytes.Length / 2);
                
                for (int i = 0; i < pairCount; i++)
                {
                    byte first = keyBytes[i * 2];
                    byte second = keyBytes[(i * 2 + 1) % keyBytes.Length];

                    if (!_connections.ContainsKey(first) && !_connections.ContainsKey(second) && first != second)
                    {
                        _connections[first] = second;
                        _connections[second] = first;
                    }
                }
            }

            public byte Transform(byte input)
            {
                return _connections.TryGetValue(input, out byte output) ? output : input;
            }
        }
    }
}
