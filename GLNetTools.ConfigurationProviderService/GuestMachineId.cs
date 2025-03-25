using System.Diagnostics.CodeAnalysis;

namespace GLNetTools.ConfigurationProviderService
{
	public readonly struct GuestMachineId : IEquatable<GuestMachineId>
	{
		public static readonly GuestMachineId UninitializedMachine = new(0);
		public static readonly GuestMachineId ContextBasedMachine = new(1);
		public static readonly GuestMachineId AnyMachine = new(2);


		public byte Id { get; }

		public bool IsUninitialized => Id == 0;

		public bool IsAny => Id == 2;

		public bool IsContextBased => Id == 1;


		public GuestMachineId(byte id)
		{
			if (id >= 3 && id < 100)
				throw new ArgumentException("Invalid id, valid are '0', '1', '2' or in range [100 - 255]", nameof(id));
			Id = id;
		}


		public GuestMachineId UseContext(GuestMachineId replacement)
		{
			return IsContextBased ? replacement : this;
		}

		public override bool Equals([NotNullWhen(true)] object? obj) => obj is GuestMachineId gm && Equals(gm);

		public bool Equals(GuestMachineId other) => other.Id == Id;

		public override int GetHashCode() => Id;

		public override string ToString() => $"[GM:{Id}]";

		public static bool operator ==(GuestMachineId a, GuestMachineId b) => a.Id == b.Id;

		public static bool operator !=(GuestMachineId a, GuestMachineId b) => a.Id != b.Id;
	}
}
