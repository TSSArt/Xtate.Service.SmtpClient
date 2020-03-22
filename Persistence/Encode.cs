﻿using System;

namespace TSSArt.StateMachine
{
	internal static class Encode
	{
		internal static int GetLength(byte val)
		{
			if ((val & 0x80) == 0x00) return 1;
			if ((val & 0xE0) == 0xC0) return 2;
			if ((val & 0xF0) == 0xE0) return 3;
			if ((val & 0xF8) == 0xF0) return 4;
			if ((val & 0xFC) == 0xF8) return 5;
			if ((val & 0xFE) == 0xFC) return 6;

			throw new ArgumentException(Resources.Exception_Incorrect_encoding, nameof(val));
		}

		internal static int Decode(ReadOnlySpan<byte> span)
		{
			switch (span.Length)
			{
				case 1: return span[0] & 0x7F;

				case 2 when (span[0] & 0xE0) == 0xC0 && (span[1] & 0xC0) == 0x80:
					return ((span[0] & 0x1F) << 6) + (span[1] & 0x3F);

				case 3 when (span[0] & 0xF0) == 0xE0 && (span[1] & 0xC0) == 0x80 && (span[2] & 0xC0) == 0x80:
					return ((span[0] & 0x0F) << 12) + ((span[1] & 0x3F) << 6) + (span[2] & 0x3F);

				case 4 when (span[0] & 0xF8) == 0xF0 && (span[1] & 0xC0) == 0x80 && (span[2] & 0xC0) == 0x80 && (span[3] & 0xC0) == 0x80:
					return ((span[0] & 0x07) << 18) + ((span[1] & 0x3F) << 12) + ((span[2] & 0x3F) << 6) + (span[3] & 0x3F);

				case 5 when (span[0] & 0xFC) == 0xF8 && (span[1] & 0xC0) == 0x80 && (span[2] & 0xC0) == 0x80 && (span[3] & 0xC0) == 0x80 && (span[4] & 0xC0) == 0x80:
					return ((span[0] & 0x03) << 24) + ((span[1] & 0x3F) << 18) + ((span[2] & 0x3F) << 12) + ((span[3] & 0x3F) << 6) + (span[4] & 0x3F);

				case 6 when (span[0] & 0xFE) == 0xFC && (span[1] & 0xC0) == 0x80 && (span[2] & 0xC0) == 0x80 && (span[3] & 0xC0) == 0x80 && (span[4] & 0xC0) == 0x80 && (span[5] & 0xC0) == 0x80:
					return ((span[0] & 0x01) << 30) + ((span[1] & 0x3F) << 24) + ((span[2] & 0x3F) << 18) + ((span[3] & 0x3F) << 12) + ((span[4] & 0x3F) << 6) + (span[5] & 0x3F);
			}

			throw new ArgumentException(Resources.Exception_Incorrect_encoding, nameof(span));
		}

		internal static int GetEncodedLength(int val)
		{
			if (val < 0) throw new ArgumentOutOfRangeException(nameof(val));

			if (val <= 0x7F) return 1;
			if (val <= 0x7FF) return 2;
			if (val <= 0xFFFF) return 3;
			if (val <= 0x1FFFFF) return 4;
			if (val <= 0x3FFFFFF) return 5;
			return 6;
		}

		private static ulong GetEncodedValue(int val)
		{
			if (val < 0) throw new ArgumentOutOfRangeException(nameof(val));

			var value = (ulong) val;

			if (value <= 0x7F)
			{
				return value;
			}

			if (value <= 0x7FF)
			{
				return 0x80C0U + (value >> 6) +
					   ((value & 0x3FU) << 8);
			}

			if (value <= 0xFFFF)
			{
				return 0x8080E0U + (value >> 12) +
					   ((value & 0xFC0U) << 2) +
					   ((value & 0x3FU) << 16);
			}

			if (value <= 0x1FFFFF)
			{
				return 0x808080F0U + (value >> 18) +
					   ((value & 0x3F000U) >> 4) +
					   ((value & 0xFC0U) << 10) +
					   ((value & 0x3FU) << 24);
			}

			if (value <= 0x3FFFFFF)
			{
				return 0x80808080F8UL + (value >> 24) +
					   ((value & 0xFC0000U) >> 10) +
					   ((value & 0x3F000U) << 4) +
					   ((value & 0xFC0U) << 18) +
					   ((value & 0x3FUL) << 32);
			}

			return 0x8080808080FCUL + (value >> 30) +
				   ((value & 0x3F000000) >> 16) +
				   ((value & 0xFC0000) >> 2) +
				   ((value & 0x3F000) << 12) +
				   ((value & 0xFC0UL) << 26) +
				   ((value & 0x3FUL) << 40);
		}

		internal static void WriteEncodedValue(Span<byte> span, int val)
		{
			var value = GetEncodedValue(val);

			for (var i = 0; i < span.Length; i ++)
			{
				span[i] = (byte) value;

				value >>= 8;
			}
		}
	}
}