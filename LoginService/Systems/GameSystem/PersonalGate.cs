namespace LoginService.Systems.GameSystem
{
    public sealed class PersonalGate
    {
        public Gate Gate { get; init; }
        public byte CharactersCount { get; init; }

        public PersonalGate(Gate gate, byte charactersCount) =>
            (Gate, CharactersCount) = (gate, charactersCount);
    }
}