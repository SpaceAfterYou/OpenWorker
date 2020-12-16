using ow.Framework.Game.Datas.Bin.Table.Entities;

namespace ow.Service.District.Game
{
    public sealed record MazeDayEventBooster
    {
        public ushort Id { get; }
        public MazeInfoTableEntity Maze { get; }

        public MazeDayEventBooster(ushort id, MazeInfoTableEntity maze) => (Id, Maze) = (id, maze);
    }
}