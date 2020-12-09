namespace ow.Service.Login.Game
{
    public sealed class PersonalGate
    {
        public GateInstance Gate { get; init; }
        public byte CharactersCount { get; init; }

        public PersonalGate(GateInstance gate, byte charactersCount) =>
            (Gate, CharactersCount) = (gate, charactersCount);
    }
}
