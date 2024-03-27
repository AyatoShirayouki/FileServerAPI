using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileShareLibrary
{
	public struct StringKey : IEquatable<StringKey>
	{
		private readonly string _value;

		public StringKey(string value)
		{
			_value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public bool Equals(StringKey other)
		{
			return _value == other._value;
		}

		public override bool Equals(object obj)
		{
			return obj is StringKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		public static implicit operator StringKey(string value)
		{
			return new StringKey(value);
		}

		public static implicit operator string(StringKey key)
		{
			return key._value;
		}

		public override string ToString()
		{
			return _value;
		}
	}
}
