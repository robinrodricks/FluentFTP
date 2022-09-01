using System;
using System.Security.Cryptography;

namespace FluentFTP.Helpers.Hashing {
	internal class CRC32 : HashAlgorithm {
		public const uint DefaultPolynomial = 0xedb88320;

		public const uint DefaultSeed = 0xffffffff;

		private uint hash;
		private readonly uint seed;
		private readonly uint[] table;
		private static uint[] defaultTable;

		public CRC32() {
			table = InitializeTable(DefaultPolynomial);
			seed = DefaultSeed;
			Initialize();
		}

		public CRC32(uint polynomial, uint seed) {
			table = InitializeTable(polynomial);
			this.seed = seed;
			Initialize();
		}

		public override void Initialize() {
			hash = seed;
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize) {
			hash = CalculateHash(table, hash, array, ibStart, cbSize);
		}

		protected override byte[] HashFinal() {
			byte[] hashBuffer = UInt32ToBigEndianBytes(~hash);
#if NETFRAMEWORK || NETSTANDARD2_0
			this.HashValue = hashBuffer;
#endif
			return hashBuffer;
		}

		public override int HashSize {
			get { return 32; }
		}

		public static uint Compute(byte[] buffer) {
			return ~CalculateHash(InitializeTable(DefaultPolynomial), DefaultSeed, buffer, 0, buffer.Length);
		}

		public static uint Compute(uint seed, byte[] buffer) {
			return ~CalculateHash(InitializeTable(DefaultPolynomial), seed, buffer, 0, buffer.Length);
		}

		public static uint Compute(uint polynomial, uint seed, byte[] buffer) {
			return ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);
		}

		private static uint[] InitializeTable(uint polynomial) {
			if (polynomial == DefaultPolynomial && defaultTable != null) {
				return defaultTable;
			}

			uint[] createTable = new uint[256];
			for (int i = 0; i < 256; i++) {
				uint entry = (uint)i;
				for (int j = 0; j < 8; j++) {
					if ((entry & 1) == 1) {
						entry = (entry >> 1) ^ polynomial;
					}
					else {
						entry >>= 1;
					}
				}

				createTable[i] = entry;
			}

			if (polynomial == DefaultPolynomial) {
				defaultTable = createTable;
			}

			return createTable;
		}

		private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size) {
			uint crc = seed;
			for (int i = start; i < size; i++) {
				unchecked {
					crc = (crc >> 8) ^ table[buffer[i] ^ (crc & 0xff)];
				}
			}

			return crc;
		}

		private byte[] UInt32ToBigEndianBytes(uint x) {
			return new[] {
				(byte)((x >> 24) & 0xff),
				(byte)((x >> 16) & 0xff),
				(byte)((x >> 8) & 0xff),
				(byte)(x & 0xff)
			};
		}
	}
}